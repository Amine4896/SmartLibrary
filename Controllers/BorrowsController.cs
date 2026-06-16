using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLibrary.Data;
using SmartLibrary.Models;

namespace SmartLibrary.Controllers
{
    [Authorize]
    public class BorrowsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BorrowsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task LogActivityAsync(string action)
        {
            var username = User.Identity?.Name ?? "Inconnu";
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Visiteur";
            
            int? userId = null;
            var userObj = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (userObj != null)
            {
                userId = userObj.Id;
            }

            var log = new ActivityLog
            {
                UserId = userId,
                Username = username,
                UserRole = role,
                Action = action,
                Timestamp = DateTime.Now
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.Borrows
                .Include(b => b.BorrowItems)
                .ThenInclude(bi => bi.Book)
                .AsQueryable();

            if (User.IsInRole("Client"))
            {
                var username = User.Identity?.Name;
                var userObj = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (userObj != null)
                {
                    query = query.Where(b => b.UserId == userObj.Id);
                }
            }

            var borrows = await query.OrderByDescending(b => b.BorrowDate).ToListAsync();
            return View(borrows);
        }

        public IActionResult Create()
        {
            ViewBag.Books = _context.Books.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string borrowerName, List<int> bookIds)
        {
            var currentUsername = User.Identity?.Name;
            var userObj = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);

            if (User.IsInRole("Client"))
            {
                borrowerName = currentUsername ?? "Client";
            }

            if (string.IsNullOrWhiteSpace(borrowerName))
            {
                ModelState.AddModelError("", "Le nom de l'emprunteur est requis.");
            }
            if (bookIds == null || !bookIds.Any())
            {
                ModelState.AddModelError("", "Veuillez sélectionner au moins un livre.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Books = await _context.Books.ToListAsync();
                return View();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var borrow = new Borrow
                {
                    BorrowerName = borrowerName,
                    UserId = userObj?.Id,
                    Status = User.IsInRole("Client") ? "Pending" : "Approved",
                    BorrowItems = new List<BorrowItem>()
                };

                var bookTitles = new List<string>();

                foreach (var bookId in bookIds ?? new List<int>())
                {
                    var book = await _context.Books.FindAsync(bookId);

                    if (book == null)
                    {
                        ModelState.AddModelError("", "Livre introuvable.");
                        ViewBag.Books = await _context.Books.ToListAsync();
                        return View();
                    }

                    if (!User.IsInRole("Client"))
                    {
                        if (book.StockQuantity <= 0)
                        {
                            ModelState.AddModelError("", $"Stock insuffisant pour le livre '{book.Title}'.");
                            ViewBag.Books = await _context.Books.ToListAsync();
                            return View();
                        }
                        book.StockQuantity--;
                    }
                    else
                    {
                        if (book.StockQuantity <= 0)
                        {
                            ModelState.AddModelError("", $"Le livre '{book.Title}' est en rupture de stock.");
                            ViewBag.Books = await _context.Books.ToListAsync();
                            return View();
                        }
                    }

                    borrow.BorrowItems.Add(new BorrowItem
                    {
                        BookId = bookId
                    });
                    bookTitles.Add(book.Title);
                }

                _context.Borrows.Add(borrow);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var titlesStr = string.Join(", ", bookTitles);
                if (User.IsInRole("Client"))
                {
                    await LogActivityAsync($"A demandé l'emprunt de {(bookIds?.Count ?? 0)} livre(s) : '{titlesStr}'");
                    TempData["SuccessMessage"] = "Votre demande d'emprunt a été soumise avec succès et est en attente d'approbation !";
                }
                else
                {
                    await LogActivityAsync($"A enregistré un emprunt approuvé pour '{borrowerName}' : '{titlesStr}'");
                    TempData["SuccessMessage"] = $"L'emprunt pour '{borrowerName}' a été enregistré avec succès !";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Une erreur est survenue lors de l'enregistrement de l'emprunt : " + ex.Message);
                ViewBag.Books = await _context.Books.ToListAsync();
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Approve(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var borrow = await _context.Borrows
                    .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Book)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (borrow == null) return NotFound();

                if (borrow.Status != "Pending")
                {
                    TempData["ErrorMessage"] = "Cette demande n'est pas en attente.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var item in borrow.BorrowItems)
                {
                    if (item.Book == null || item.Book.StockQuantity <= 0)
                    {
                        TempData["ErrorMessage"] = $"Stock insuffisant pour approuver l'emprunt du livre '{item.Book?.Title ?? "inconnu"}'.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                foreach (var item in borrow.BorrowItems)
                {
                    if (item.Book != null)
                    {
                        item.Book.StockQuantity--;
                    }
                }

                borrow.Status = "Approved";
                borrow.BorrowDate = DateTime.Now;
                borrow.DueDate = DateTime.Now.AddDays(14);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var bookNames = string.Join(", ", borrow.BorrowItems.Select(bi => bi.Book?.Title ?? "Inconnu"));
                await LogActivityAsync($"A approuvé la demande d'emprunt de '{borrow.BorrowerName}' pour : '{bookNames}'");
                TempData["SuccessMessage"] = $"La demande d'emprunt de '{borrow.BorrowerName}' a été approuvée avec succès !";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Une erreur est survenue lors de l'approbation : " + ex.Message;
            }

            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var borrow = await _context.Borrows
                    .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Book)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (borrow == null) return NotFound();

                if (borrow.Status != "Pending")
                {
                    TempData["ErrorMessage"] = "Cette demande n'est pas en attente.";
                    return RedirectToAction(nameof(Index));
                }

                borrow.Status = "Rejected";
                await _context.SaveChangesAsync();

                var bookNames = string.Join(", ", borrow.BorrowItems.Select(bi => bi.Book?.Title ?? "Inconnu"));
                await LogActivityAsync($"A rejeté la demande d'emprunt de '{borrow.BorrowerName}' pour : '{bookNames}'");
                TempData["SuccessMessage"] = $"La demande d'emprunt de '{borrow.BorrowerName}' a été rejetée.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Une erreur est survenue lors du rejet : " + ex.Message;
            }

            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Client")]
        public async Task<IActionResult> ReturnBook(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var borrow = await _context.Borrows
                    .Include(b => b.BorrowItems)
                    .ThenInclude(bi => bi.Book)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (borrow == null)
                {
                    return NotFound();
                }

                if (User.IsInRole("Client"))
                {
                    var username = User.Identity?.Name;
                    var userObj = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                    if (userObj == null || borrow.UserId != userObj.Id)
                    {
                        TempData["ErrorMessage"] = "Vous n'êtes pas autorisé à retourner cet emprunt.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                if (borrow.Status == "Returned")
                {
                    TempData["ErrorMessage"] = "Ces livres ont déjà été retournés.";
                    return RedirectToAction(nameof(Index));
                }

                borrow.ReturnDate = DateTime.Now;
                borrow.Status = "Returned";

                foreach (var item in borrow.BorrowItems)
                {
                    if (item.Book != null)
                    {
                        item.Book.StockQuantity++;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var bookNames = string.Join(", ", borrow.BorrowItems.Select(bi => bi.Book?.Title ?? "Inconnu"));
                await LogActivityAsync($"A enregistré le retour des livres de '{borrow.BorrowerName}' : '{bookNames}'");
                TempData["SuccessMessage"] = $"Les livres empruntés par '{borrow.BorrowerName}' ont été retournés avec succès !";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Une erreur est survenue lors du retour : " + ex.Message;
            }

            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }
            return RedirectToAction(nameof(Index));
        }
    }

}