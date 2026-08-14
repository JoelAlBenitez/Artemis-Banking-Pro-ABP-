using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            if (User.IsInRole("Administrador"))
                return RedirectToAction("Index", "Admin");
            
            if (User.IsInRole("Cajero"))
                return RedirectToAction("Index", "CashierHome");
            
            if (User.IsInRole("Cliente"))
                return RedirectToAction("Index", "Customer");

            // Si es un rol sin dashboard web (e.g. Comercio)
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Privacy()
        {
            return View();
        }

    
    }
}
