using ArtemisBankingPro.Core.Domain.Common.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers
{
    [Authorize(Roles = nameof(Roles.Cliente))]
    public class ClientHomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
