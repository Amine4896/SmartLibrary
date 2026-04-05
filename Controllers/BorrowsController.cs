using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLibrary.Data;
using SmartLibrary.Models;

namespace SmartLibrary.Controllers
{
    public class BorrowsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BorrowsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Create()
        {
            ViewBag.Books = _context.Books.ToList();
            return View();
        }
        public IActionResult Index()
{
    var borrows = _context.Borrows
        .Include(b => b.BorrowItems)
        .ThenInclude(bi => bi.Book)
        .ToList();

    return View(borrows);
}

        [HttpPost]
        public async Task<IActionResult> Create(string borrowerName, List<int> bookIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var borrow = new Borrow
                {
                    BorrowerName = borrowerName,
                    BorrowItems = new List<BorrowItem>()
                };

                foreach (var bookId in bookIds)
                {
                    var book = await _context.Books.FindAsync(bookId);

                    if (book == null || book.StockQuantity <= 0)
                        return Content("Stock insuffisant ❌");

                    book.StockQuantity--;

                    borrow.BorrowItems.Add(new BorrowItem
                    {
                        BookId = bookId
                    });
                }

                _context.Borrows.Add(borrow);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return RedirectToAction("Index", "Books");
            }
            catch
            {
                await transaction.RollbackAsync();
                return Content("Erreur ❌");
            }
        }
    }

}