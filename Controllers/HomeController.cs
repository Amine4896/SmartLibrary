using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authorization;
using SmartLibrary.Data;
using SmartLibrary.Models;

namespace SmartLibrary.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IHostApplicationLifetime _appLifetime;

    public HomeController(ApplicationDbContext context, IHostApplicationLifetime appLifetime)
    {
        _context = context;
        _appLifetime = appLifetime;
    }

    // Trigger Jenkins & SonarQube validation pipeline
    public async Task<IActionResult> Index()
    {
        var username = User.Identity?.Name;
        User? userObj = null;
        if (!string.IsNullOrEmpty(username))
        {
            userObj = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        if (User.IsInRole("Client") && userObj != null)
        {
            var userBorrows = await _context.Borrows
                .Include(b => b.BorrowItems)
                .ThenInclude(bi => bi.Book)
                .Where(b => b.UserId == userObj.Id)
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            ViewBag.UserBorrows = userBorrows;
            ViewBag.TotalClientBorrows = userBorrows.Count;
            ViewBag.ActiveClientBorrows = userBorrows.Count(b => b.Status == "Approved");
            ViewBag.PendingClientBorrows = userBorrows.Count(b => b.Status == "Pending");
        }
        else
        {
            ViewBag.TotalBooks = await _context.Books.CountAsync();
            ViewBag.AvailableBooks = await _context.Books.SumAsync(b => (int?)b.StockQuantity) ?? 0;
            ViewBag.BorrowedBooks = await _context.Borrows.CountAsync(b => b.Status == "Approved");
            ViewBag.TotalCategories = await _context.Categories.CountAsync();

            ViewBag.RecentBorrows = await _context.Borrows
                .Include(b => b.BorrowItems)
                .ThenInclude(bi => bi.Book)
                .OrderByDescending(b => b.BorrowDate)
                .Take(5)
                .ToListAsync();

            if (User.Identity != null && User.Identity.IsAuthenticated && (User.IsInRole("Admin") || User.IsInRole("Manager")))
            {
                ViewBag.PendingBorrows = await _context.Borrows
                    .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Book)
                    .Where(b => b.Status == "Pending")
                    .OrderByDescending(b => b.BorrowDate)
                    .ToListAsync();
            }

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                ViewBag.FeaturedBooks = await _context.Books
                    .Include(b => b.Category)
                    .OrderBy(b => b.Title)
                    .Take(6)
                    .ToListAsync();
            }

            if (User.IsInRole("Admin"))
            {
                // Fetch daily stats (only approved or returned borrows count towards revenue/count)
                var dailyStats = await _context.Borrows
                    .Where(b => b.Status == "Approved" || b.Status == "Returned")
                    .SelectMany(b => b.BorrowItems.Select(bi => new
                    {
                        Date = b.BorrowDate.Date,
                        Price = bi.Book != null ? bi.Book.Price : 0
                    }))
                    .GroupBy(x => x.Date)
                    .Select(g => new DailyRevenueViewModel
                    {
                        Date = g.Key,
                        BorrowedBooksCount = g.Count(),
                        Revenue = g.Sum(x => x.Price)
                    })
                    .OrderByDescending(d => d.Date)
                    .Take(10)
                    .ToListAsync();

                ViewBag.DailyStats = dailyStats;
            }
        }

        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StopApp()
    {
        var username = User.Identity?.Name ?? "Inconnu";
        int? userId = null;
        var userObj = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (userObj != null)
        {
            userId = userObj.Id;
        }

        // Log system shutdown action
        var log = new ActivityLog
        {
            UserId = userId,
            Username = username,
            UserRole = "Admin",
            Action = "A arrêté l'application (fermeture du serveur)",
            Timestamp = DateTime.Now
        };
        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();

        // Trigger shutdown with a small delay so the confirmation page renders
        _ = Task.Run(async () =>
        {
            await Task.Delay(1500);
            _appLifetime.StopApplication();
        });

        return View("AppStopped");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
