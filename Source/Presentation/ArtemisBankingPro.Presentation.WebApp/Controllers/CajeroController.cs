using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.ViewModels.Cajero;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers
{
    [Authorize(Roles = "Cajero")]
    public class CajeroController : Controller
    {
        private readonly ITransaccionCajeroService _transaccionCajeroService;

        public CajeroController(ITransaccionCajeroService transaccionCajeroService)
        {
            _transaccionCajeroService = transaccionCajeroService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var indicadores = await _transaccionCajeroService.ObtenerIndicadoresHoyAsync(userId);
            return View(indicadores);
        }
    }
}
