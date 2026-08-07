using System.Security.Claims;
using Artemis_Banking_Pro.Core.Application.Contracts.Dashboard;
using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using Artemis_Banking_Pro.Core.Application.ViewModels.Transactions;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers.Transactions
{
    [Authorize(Roles = "Cliente")]
    public class CashAdvanceController : Controller
    {
        private readonly ICashAdvanceServices _cashAdvanceServices;
        private readonly IDashboardService _dashboardService;
        private readonly IMapper _mapper;

        public CashAdvanceController(
            ICashAdvanceServices cashAdvanceServices,
            IDashboardService dashboardService,
            IMapper mapper)
        {
            _cashAdvanceServices = cashAdvanceServices;
            _dashboardService = dashboardService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            var vm = new CashAdvanceViewModel { Amount = 0 };
            await PopulateViewModelListsAsync(vm, clientId);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CashAdvanceViewModel vm)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                await PopulateViewModelListsAsync(vm, clientId);
                return View(vm);
            }

            var dto = _mapper.Map<CashAdvanceRequestDto>(vm);
            var result = await _cashAdvanceServices.ProcessCashAdvanceAsync(dto, clientId);

            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await PopulateViewModelListsAsync(vm, clientId);
                return View(vm);
            }

            TempData["SuccessMessage"] = "El avance de efectivo ha sido procesado exitosamente.";
            return RedirectToAction("Index", "Customer");
        }

        private async Task PopulateViewModelListsAsync(CashAdvanceViewModel vm, string clientId)
        {
            var dashboardResult = await _dashboardService.GetClientDashboardAsync(clientId);
            vm.AvailableCards = dashboardResult.Value?.CreditCards 
                ?? new List<Artemis_Banking_Pro.Core.Application.DTOs.CreditCards.CreditCardDto>();
            vm.AvailableAccounts = dashboardResult.Value?.SavingsAccounts 
                ?? new List<Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts.SavingsAccountDto>();
        }
    }
}
