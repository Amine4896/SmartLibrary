using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using SmartLibrary.Data;
using SmartLibrary.Models;
using SmartLibrary.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

// Groq AI Service
builder.Services.AddHttpClient<GroqService>();
builder.Services.AddScoped<BookService>();
builder.Services.AddScoped<LibraryAssistantService>();

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        if (!context.Users.Any())
        {
            var hasher = new PasswordHasher<User>();
            
            var admin = new User
            {
                Username = "admin",
                PasswordHash = "",
                Role = "Admin"
            };
            admin.PasswordHash = hasher.HashPassword(admin, "admin123");

            var manager = new User
            {
                Username = "manager",
                PasswordHash = "",
                Role = "Manager"
            };
            manager.PasswordHash = hasher.HashPassword(manager, "manager123");

            var client = new User
            {
                Username = "amine",
                PasswordHash = "",
                Role = "Client"
            };
            client.PasswordHash = hasher.HashPassword(client, "amine123");

            context.Users.AddRange(admin, manager, client);
            context.SaveChanges();
        }
        else if (!context.Users.Any(u => u.Username == "amine"))
        {
            var hasher = new PasswordHasher<User>();
            var client = new User
            {
                Username = "amine",
                PasswordHash = "",
                Role = "Client"
            };
            client.PasswordHash = hasher.HashPassword(client, "amine123");
            context.Users.Add(client);
            context.SaveChanges();
        }

        // Clean up previous unrelated mock data to ensure a clean real library setup
        if (context.Categories.Any(c => c.Name == "MATH" || c.Name == "Mosquée" || c.Name == "Restaurant"))
        {
            context.BorrowItems.RemoveRange(context.BorrowItems);
            context.Borrows.RemoveRange(context.Borrows);
            context.Books.RemoveRange(context.Books);
            context.Categories.RemoveRange(context.Categories);
            context.SaveChanges();
        }

        if (!context.Categories.Any())
        {
            var dev = new Category { Name = "Informatique & Programmation" };
            var sf = new Category { Name = "Science-Fiction & Fantaisie" };
            var classique = new Category { Name = "Littérature Classique" };
            var perso = new Category { Name = "Développement Personnel" };
            var histoire = new Category { Name = "Histoire & Biographies" };

            context.Categories.AddRange(dev, sf, classique, perso, histoire);
            context.SaveChanges();
        }

        if (!context.Books.Any())
        {
            var dev = context.Categories.FirstOrDefault(c => c.Name == "Informatique & Programmation");
            var sf = context.Categories.FirstOrDefault(c => c.Name == "Science-Fiction & Fantaisie");
            var classique = context.Categories.FirstOrDefault(c => c.Name == "Littérature Classique");
            var perso = context.Categories.FirstOrDefault(c => c.Name == "Développement Personnel");
            var histoire = context.Categories.FirstOrDefault(c => c.Name == "Histoire & Biographies");

            if (dev != null && sf != null && classique != null && perso != null && histoire != null)
            {
                context.Books.AddRange(
                    new Book { Title = "C# 12 and .NET 8 – Modern Cross-Platform Development", Author = "Mark J. Price", Price = 49.99m, StockQuantity = 5, CategoryId = dev.Id },
                    new Book { Title = "Clean Code: A Handbook of Agile Software Craftsmanship", Author = "Robert C. Martin", Price = 39.99m, StockQuantity = 3, CategoryId = dev.Id },
                    new Book { Title = "Design Patterns: Elements of Reusable Object-Oriented Software", Author = "Erich Gamma", Price = 45.50m, StockQuantity = 2, CategoryId = dev.Id },
                    new Book { Title = "Le Seigneur des Anneaux", Author = "J.R.R. Tolkien", Price = 25.00m, StockQuantity = 4, CategoryId = sf.Id },
                    new Book { Title = "Dune", Author = "Frank Herbert", Price = 12.90m, StockQuantity = 6, CategoryId = sf.Id },
                    new Book { Title = "Fondation", Author = "Isaac Asimov", Price = 9.99m, StockQuantity = 3, CategoryId = sf.Id },
                    new Book { Title = "Le Petit Prince", Author = "Antoine de Saint-Exupéry", Price = 7.50m, StockQuantity = 10, CategoryId = classique.Id },
                    new Book { Title = "Les Misérables", Author = "Victor Hugo", Price = 15.00m, StockQuantity = 2, CategoryId = classique.Id },
                    new Book { Title = "1984", Author = "George Orwell", Price = 8.50m, StockQuantity = 8, CategoryId = classique.Id },
                    new Book { Title = "Atomic Habits", Author = "James Clear", Price = 18.00m, StockQuantity = 5, CategoryId = perso.Id },
                    new Book { Title = "Les 7 habitudes de ceux qui réalisent tout ce qu'ils entreprennent", Author = "Stephen Covey", Price = 22.00m, StockQuantity = 4, CategoryId = perso.Id },
                    new Book { Title = "Sapiens : Une brève histoire de l'humanité", Author = "Yuval Noah Harari", Price = 24.90m, StockQuantity = 7, CategoryId = histoire.Id },
                    new Book { Title = "Steve Jobs", Author = "Walter Isaacson", Price = 21.00m, StockQuantity = 3, CategoryId = histoire.Id }
                );
                context.SaveChanges();
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Une erreur est survenue lors de l'initialisation de la base.");
    }
}

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();