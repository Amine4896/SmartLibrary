using Microsoft.EntityFrameworkCore;
using SmartLibrary.Models;

namespace SmartLibrary.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Borrow> Borrows { get; set; }
        public DbSet<BorrowItem> BorrowItems { get; set; }
    }
}