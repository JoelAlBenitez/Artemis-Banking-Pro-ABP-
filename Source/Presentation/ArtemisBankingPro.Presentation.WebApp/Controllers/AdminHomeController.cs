using ArtemisBankingPro.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers
{
    [Authorize(Roles = Roles.Administrador)]
    public class AdminHomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
