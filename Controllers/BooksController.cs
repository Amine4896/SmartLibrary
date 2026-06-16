using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartLibrary.Data;
using SmartLibrary.Models;

namespace SmartLibrary.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
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

        // GET: Books
        public async Task<IActionResult> Index(string searchString)
{
    var books = _context.Books
        .Include(b => b.Category)
        .AsNoTracking()
        .AsQueryable();

    if (!string.IsNullOrEmpty(searchString))
    {
        books = books.Where(b =>
            b.Title.Contains(searchString) ||
            b.Author.Contains(searchString));
    }

    return View(await books.ToListAsync());
}

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .Include(b => b.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null) return NotFound();

            return View(book);
        }

        // GET: Books/Create
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(Book book)
        {
            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", book.CategoryId);
                return View(book);
            }

            _context.Add(book);
            await _context.SaveChangesAsync();
            await LogActivityAsync($"A créé le livre '{book.Title}' (Stock: {book.StockQuantity}, Catégorie ID: {book.CategoryId})");
            TempData["SuccessMessage"] = $"Le livre '{book.Title}' a été ajouté avec succès !";
            return RedirectToAction(nameof(Index));
        }

        // GET: Books/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", book.CategoryId);
            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Book book)
        {
            if (id != book.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                    await LogActivityAsync($"A modifié le livre '{book.Title}' (Stock: {book.StockQuantity}, Catégorie ID: {book.CategoryId})");
                    TempData["SuccessMessage"] = $"Le livre '{book.Title}' a été modifié avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Books.Any(e => e.Id == book.Id))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", book.CategoryId);
            return View(book);
        }

        // GET: Books/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .Include(b => b.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null) return NotFound();

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);

            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
                await LogActivityAsync($"A supprimé le livre '{book.Title}' (Auteur: {book.Author})");
                TempData["SuccessMessage"] = $"Le livre '{book.Title}' a été supprimé avec succès !";
            }

            return RedirectToAction(nameof(Index));
        }
        
    }
}