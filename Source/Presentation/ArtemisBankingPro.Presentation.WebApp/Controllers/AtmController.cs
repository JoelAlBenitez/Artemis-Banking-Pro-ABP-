using ArtemisBankingPro.Core.Application.ViewModels.Atm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers
{
    [Authorize(Roles = "Cajero")]
    public class AtmController : Controller
    {
        public AtmController()
        {
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // TODO: Fetch indicators from TransactionService (commented out to avoid dependency issues)
            // var indicadores = await _transactionService.GetTodayIndicatorsAsync(userId);
            
            return View(); // Assuming the view will handle a null model or a mocked model for now
        }
    }
}
