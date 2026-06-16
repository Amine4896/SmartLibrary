using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLibrary.Data;
using SmartLibrary.Models;

namespace SmartLibrary.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
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

        // GET: Users/Logs
        public async Task<IActionResult> Logs()
        {
            var logs = await _context.ActivityLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
            return View(logs);
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var managers = await _context.Users
                .Where(u => u.Role == "Manager")
                .ToListAsync();
            return View(managers);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Le nom d'utilisateur et le mot de passe sont requis.");
                return View();
            }

            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                ModelState.AddModelError("Username", "Ce nom d'utilisateur est déjà pris.");
                return View();
            }

            var manager = new User
            {
                Username = username,
                PasswordHash = "",
                Role = "Manager"
            };

            var hasher = new PasswordHasher<User>();
            manager.PasswordHash = hasher.HashPassword(manager, password);

            _context.Users.Add(manager);
            await _context.SaveChangesAsync();
            await LogActivityAsync($"A créé le compte Manager '{username}'");

            TempData["SuccessMessage"] = $"Le compte Manager '{username}' a été créé avec succès !";
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != "Manager") return NotFound();

            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string username, string? newPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != "Manager") return NotFound();

            if (string.IsNullOrWhiteSpace(username))
            {
                ModelState.AddModelError("Username", "Le nom d'utilisateur est requis.");
                return View(user);
            }

            if (user.Username != username && await _context.Users.AnyAsync(u => u.Username == username))
            {
                ModelState.AddModelError("Username", "Ce nom d'utilisateur est déjà pris.");
                return View(user);
            }

            user.Username = username;

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                var hasher = new PasswordHasher<User>();
                user.PasswordHash = hasher.HashPassword(user, newPassword);
            }

            _context.Update(user);
            await _context.SaveChangesAsync();
            await LogActivityAsync($"A mis à jour le compte Manager '{username}'");

            TempData["SuccessMessage"] = $"Le compte Manager '{username}' a été mis à jour avec succès !";
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != "Manager") return NotFound();

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != "Manager") return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            await LogActivityAsync($"A supprimé le compte Manager '{user.Username}'");

            TempData["SuccessMessage"] = $"Le compte Manager '{user.Username}' a été supprimé avec succès !";
            return RedirectToAction(nameof(Index));
        }
    }
}
