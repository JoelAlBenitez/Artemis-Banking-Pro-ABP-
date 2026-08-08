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
        private readonly ITransactionsValidationServices _validationServices;
        private readonly IMapper _mapper;

        public AccountTransferController(
            ITransactionService transactionService,
            IDashboardService dashboardService,
            ITransactionsValidationServices validationServices,
            IMapper mapper)
        {
            _transactionService = transactionService;
            _dashboardService = dashboardService;
            _validationServices = validationServices;
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
            var validation = await _validationServices.ValidateAccountTransferAsync(dto, clientId);

            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await PopulateViewModelListsAsync(vm, clientId);
                return View(vm);
            }

            var confirmVm = new ConfirmAccountTransferViewModel
            {
                SourceAccountId = vm.SourceAccountId,
                SourceAccountNumber = validation.Value.Origin.AccountNumber,
                DestinationAccountId = vm.DestinationAccountId,
                DestinationAccountNumber = validation.Value.Destination.AccountNumber,
                Amount = vm.Amount
            };

            return View("Confirm", confirmVm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Execute(ConfirmAccountTransferViewModel vm)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Datos de confirmación inválidos.";
                return RedirectToAction(nameof(Index));
            }

            var dto = _mapper.Map<AccountTransferDto>(vm);
            var result = await _transactionService.ProcessAccountTransferAsync(dto, clientId);

            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
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
            var accountsDto = dashboardResult.Value?.SavingsAccounts 
                ?? new List<Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts.SavingsAccountDto>();
            
            vm.AvailableAccounts = _mapper.Map<IReadOnlyCollection<SavingsAccountSelectViewModel>>(accountsDto);
        }
    }
}
