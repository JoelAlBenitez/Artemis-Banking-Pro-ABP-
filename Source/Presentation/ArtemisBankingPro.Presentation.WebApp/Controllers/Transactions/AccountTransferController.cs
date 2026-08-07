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
    public class AccountTransferController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly IDashboardService _dashboardService;
        private readonly IMapper _mapper;

        public AccountTransferController(
            ITransactionService transactionService,
            IDashboardService dashboardService,
            IMapper mapper)
        {
            _transactionService = transactionService;
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

            var vm = new AccountTransferViewModel { Amount = 0 };
            await PopulateViewModelListsAsync(vm, clientId);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AccountTransferViewModel vm)
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

            var dto = _mapper.Map<AccountTransferDto>(vm);
            var result = await _transactionService.ProcessAccountTransferAsync(dto, clientId);

            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await PopulateViewModelListsAsync(vm, clientId);
                return View(vm);
            }

            if (!string.IsNullOrEmpty(result.Value?.WarningMessage))
            {
                TempData["WarningMessage"] = result.Value.WarningMessage;
            }

            TempData["SuccessMessage"] = "La transferencia entre cuentas propias ha sido realizada exitosamente.";
            return RedirectToAction("Index", "Customer");
        }

        private async Task PopulateViewModelListsAsync(AccountTransferViewModel vm, string clientId)
        {
            var dashboardResult = await _dashboardService.GetClientDashboardAsync(clientId);
            vm.AvailableAccounts = dashboardResult.Value?.SavingsAccounts 
                ?? new List<Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts.SavingsAccountDto>();
        }
    }
}
