using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLibrary.Data;
using SmartLibrary.Models;
using System.Security.Claims;

namespace SmartLibrary.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task LogActivityAsync(string username, string role, string action, int? userId = null)
        {
            if (userId == null)
            {
                var userObj = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (userObj != null)
                {
                    userId = userObj.Id;
                }
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

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Nom d'utilisateur et mot de passe requis.");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                ModelState.AddModelError("", "Identifiants incorrects.");
                return View();
            }

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Identifiants incorrects.");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            await LogActivityAsync(user.Username, user.Role, "S'est connecté à l'application", user.Id);

            TempData["SuccessMessage"] = $"Bienvenue, {user.Username} ({user.Role}) !";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Tous les champs sont requis.");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Les mots de passe ne correspondent pas.");
                return View();
            }

            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                ModelState.AddModelError("Username", "Ce nom d'utilisateur est déjà pris.");
                return View();
            }

            var client = new User
            {
                Username = username,
                PasswordHash = "",
                Role = "Client"
            };

            var hasher = new PasswordHasher<User>();
            client.PasswordHash = hasher.HashPassword(client, password);

            _context.Users.Add(client);
            await _context.SaveChangesAsync();

            // Auto Sign In
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, client.Username),
                new Claim(ClaimTypes.Role, client.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            await LogActivityAsync(client.Username, client.Role, "A créé son compte Client et s'est connecté", client.Id);

            TempData["SuccessMessage"] = $"Votre compte a été créé avec succès, bienvenue {username} !";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name ?? "Inconnu";
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Visiteur";

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            await LogActivityAsync(username, role, "S'est déconnecté de l'application");

            TempData["SuccessMessage"] = "Vous vous êtes déconnecté avec succès.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
