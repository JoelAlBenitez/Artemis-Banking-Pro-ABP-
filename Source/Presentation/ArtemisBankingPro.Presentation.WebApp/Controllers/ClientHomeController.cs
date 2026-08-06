using ArtemisBankingPro.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers
{
    [Authorize(Roles = Roles.Cliente)]
    public class ClientHomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
