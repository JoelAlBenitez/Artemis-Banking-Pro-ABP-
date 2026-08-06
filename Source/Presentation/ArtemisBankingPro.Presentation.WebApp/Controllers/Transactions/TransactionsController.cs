using System.Security.Claims;
using Artemis_Banking_Pro.Core.Application.Contracts.Dashboard;
using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using Artemis_Banking_Pro.Core.Application.ViewModels.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.Beneficiaries;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers.Transactions
{
    [Authorize(Roles = "Cliente")]
    public class TransactionsController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly IPaymentService _paymentService;
        private readonly IDashboardService _dashboardService;
        private readonly IBeneficiaryRepository _beneficiaryRepository;
        private readonly IMapper _mapper;

        public TransactionsController(
            ITransactionService transactionService,
            IPaymentService paymentService,
            IDashboardService dashboardService,
            IBeneficiaryRepository beneficiaryRepository,
            IMapper mapper)
        {
            _transactionService = transactionService;
            _paymentService = paymentService;
            _dashboardService = dashboardService;
            _beneficiaryRepository = beneficiaryRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Express()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            await PopulateSavingsAccountsAsync(clientId);
            return View(new ExpressTransactionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Express(ExpressTransactionViewModel vm)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                await PopulateSavingsAccountsAsync(clientId);
                return View(vm);
            }

            var dto = _mapper.Map<ExpressTransactionDto>(vm);
            var result = await _transactionService.ProcessExpressAsync(dto, clientId);

            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await PopulateSavingsAccountsAsync(clientId);
                return View(vm);
            }

            TempData["SuccessMessage"] = "La transferencia exprés ha sido procesada exitosamente.";
            return RedirectToAction("Index", "Customer");
        }

        [HttpGet]
        public async Task<IActionResult> Beneficiary()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            await PopulateSavingsAccountsAsync(clientId);
            await PopulateBeneficiariesAsync(clientId);
            return View(new BeneficiaryTransactionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Beneficiary(BeneficiaryTransactionViewModel vm)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                await PopulateSavingsAccountsAsync(clientId);
                await PopulateBeneficiariesAsync(clientId);
                return View(vm);
            }

            var dto = _mapper.Map<BeneficiaryTransactionDto>(vm);
            var result = await _transactionService.ProcessBeneficiaryTransactionAsync(dto, clientId);

            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await PopulateSavingsAccountsAsync(clientId);
                await PopulateBeneficiariesAsync(clientId);
                return View(vm);
            }

            TempData["SuccessMessage"] = "La transferencia al beneficiario ha sido procesada exitosamente.";
            return RedirectToAction("Index", "Customer");
        }

        [HttpGet]
        public async Task<IActionResult> PayCard()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            await PopulateSavingsAccountsAsync(clientId);
            await PopulateCreditCardsAsync(clientId);
            return View(new PayCardViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayCard(PayCardViewModel vm)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                await PopulateSavingsAccountsAsync(clientId);
                await PopulateCreditCardsAsync(clientId);
                return View(vm);
            }

            var dto = _mapper.Map<PayCreditCardDto>(vm);
            var result = await _paymentService.PayCreditCardAsync(dto, clientId);

            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await PopulateSavingsAccountsAsync(clientId);
                await PopulateCreditCardsAsync(clientId);
                return View(vm);
            }

            TempData["SuccessMessage"] = "El pago de la tarjeta de crédito ha sido procesado exitosamente.";
            return RedirectToAction("Index", "Customer");
        }

        [HttpGet]
        public async Task<IActionResult> PayLoan()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            await PopulateSavingsAccountsAsync(clientId);
            await PopulateLoansAsync(clientId);
            return View(new PayLoanViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayLoan(PayLoanViewModel vm)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                await PopulateSavingsAccountsAsync(clientId);
                await PopulateLoansAsync(clientId);
                return View(vm);
            }

            var dto = _mapper.Map<PayLoanDto>(vm);
            var result = await _paymentService.PayLoanAsync(dto, clientId);

            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await PopulateSavingsAccountsAsync(clientId);
                await PopulateLoansAsync(clientId);
                return View(vm);
            }

            TempData["SuccessMessage"] = "El pago del préstamo ha sido procesado exitosamente.";
            return RedirectToAction("Index", "Customer");
        }

        private async Task PopulateSavingsAccountsAsync(string clientId)
        {
            var dashboardResult = await _dashboardService.GetClientDashboardAsync(clientId);
            ViewBag.SavingsAccounts = dashboardResult.Value?.SavingsAccounts 
                ?? new List<Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts.SavingsAccountDto>();
        }

        private async Task PopulateBeneficiariesAsync(string clientId)
        {
            var beneficiaries = await _beneficiaryRepository.GetAllFindAsync(b => b.OwnerClientId == clientId && b.IsActive);
            ViewBag.Beneficiaries = beneficiaries;
        }

        private async Task PopulateCreditCardsAsync(string clientId)
        {
            var dashboardResult = await _dashboardService.GetClientDashboardAsync(clientId);
            ViewBag.CreditCards = dashboardResult.Value?.CreditCards 
                ?? new List<Artemis_Banking_Pro.Core.Application.DTOs.CreditCards.CreditCardDto>();
        }

        private async Task PopulateLoansAsync(string clientId)
        {
            var dashboardResult = await _dashboardService.GetClientDashboardAsync(clientId);
            ViewBag.Loans = dashboardResult.Value?.Loans 
                ?? new List<Artemis_Banking_Pro.Core.Application.DTOs.Loans.LoansDto>();
        }
    }
}
