using Microsoft.AspNetCore.Mvc;

namespace SmartLibrary.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            ViewBag.Message =
            _configuration["EnvironmentMessage"];

            return View();
        }
    }
}