using System.Security.Claims;
using Artemis_Banking_Pro.Core.Application.Contracts.Dashboard;
using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using Artemis_Banking_Pro.Core.Application.ViewModels.Transactions;
using Artemis_Banking_Pro.Core.Application.Contracts.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.ViewModels.Beneficiaries;
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
        private readonly IBeneficiaryServices _beneficiaryServices;
        private readonly IMapper _mapper;

        public TransactionsController(
            ITransactionService transactionService,
            IPaymentService paymentService,
            IDashboardService dashboardService,
            IBeneficiaryServices beneficiaryServices,
            IMapper mapper)
        {
            _transactionService = transactionService;
            _paymentService = paymentService;
            _dashboardService = dashboardService;
            _beneficiaryServices = beneficiaryServices;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Express()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId)) return RedirectToAction("Login", "Account");
            await PopulateSavingsAccountsAsync(clientId);
            return View(new ExpressTransactionViewModel { SourceAccountNumber = "", DestinationAccountNumber = "" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Express(ExpressTransactionViewModel vm)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId)) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) { await PopulateSavingsAccountsAsync(clientId); return View(vm); }

            var destResult = await _atmTransactionService.GetAtmAccountDetailsAsync(vm.DestinationAccountNumber);
            if (!destResult.IsValid)
            {
                ModelState.AddModelError(string.Empty, "La cuenta destino no existe o no es válida.");
                await PopulateSavingsAccountsAsync(clientId); return View(vm);
            }

            var sourceResult = await _atmTransactionService.GetAtmAccountDetailsAsync(vm.SourceAccountNumber);
            if (!sourceResult.IsValid)
            {
                ModelState.AddModelError(string.Empty, "La cuenta origen no existe o no es válida.");
                await PopulateSavingsAccountsAsync(clientId); return View(vm);
            }
            if (sourceResult.Value!.Balance < vm.Amount)
            {
                ModelState.AddModelError(string.Empty, "Fondos insuficientes en la cuenta origen.");
                await PopulateSavingsAccountsAsync(clientId); return View(vm);
            }

            var confirmModel = new ConfirmExpressViewModel
            {
                SourceAccountNumber = vm.SourceAccountNumber,
                OriginOwnerName = sourceResult.Value!.OwnerName,
                DestinationAccountNumber = vm.DestinationAccountNumber,
                DestinationOwnerName = destResult.Value!.OwnerName,
                Amount = vm.Amount
            };

            return View("ConfirmExpress", confirmModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteExpress(ConfirmExpressViewModel vm)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId)) return RedirectToAction("Login", "Account");
            
            var dto = new ExpressTransactionDto 
            { 
                SourceAccountNumber = vm.SourceAccountNumber, 
                DestinationAccountNumber = vm.DestinationAccountNumber, 
                Amount = vm.Amount 
            };
            
            var result = await _transactionService.ProcessExpressAsync(dto, clientId);
            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction("Express");
            }
            TempData["SuccessMessage"] = "La transferencia exprés ha sido procesada exitosamente.";
            return RedirectToAction("Index", "Customer");
        }

        [HttpGet]
        public async Task<IActionResult> Beneficiary()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId)) return RedirectToAction("Login", "Account");
            await PopulateSavingsAccountsAsync(clientId);
            await PopulateBeneficiariesAsync(clientId);
            return View(new BeneficiaryTransactionViewModel { SourceAccountNumber = "" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Beneficiary(BeneficiaryTransactionViewModel vm)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId)) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) { await PopulateSavingsAccountsAsync(clientId); await PopulateBeneficiariesAsync(clientId); return View(vm); }

            var sourceResult = await _atmTransactionService.GetAtmAccountDetailsAsync(vm.SourceAccountNumber);
            if (!sourceResult.IsValid || sourceResult.Value!.Balance < vm.Amount)
            {
                ModelState.AddModelError(string.Empty, "La cuenta origen no es válida o tiene fondos insuficientes.");
                await PopulateSavingsAccountsAsync(clientId); await PopulateBeneficiariesAsync(clientId); return View(vm);
            }

            var beneficiaries = await _beneficiaryRepository.GetAllFindAsync(b => b.OwnerClientId == clientId && b.IsActive);
            var beneficiary = beneficiaries.FirstOrDefault(b => b.Id == vm.BeneficiaryId);
            if (beneficiary == null)
            {
                ModelState.AddModelError(string.Empty, "El beneficiario seleccionado no es válido.");
                await PopulateSavingsAccountsAsync(clientId); await PopulateBeneficiariesAsync(clientId); return View(vm);
            }

            var destResult = await _atmTransactionService.GetAtmAccountDetailsAsync(beneficiary.BeneficiaryAccountNumber);
            
            var confirmModel = new ConfirmBeneficiaryViewModel
            {
                BeneficiaryId = vm.BeneficiaryId,
                SourceAccountNumber = vm.SourceAccountNumber,
                OriginOwnerName = sourceResult.Value!.OwnerName,
                DestinationAccountNumber = beneficiary.BeneficiaryAccountNumber,
                DestinationOwnerName = destResult.IsValid ? destResult.Value!.OwnerName : "Beneficiario",
                Amount = vm.Amount
            };

            return View("ConfirmBeneficiary", confirmModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteBeneficiary(ConfirmBeneficiaryViewModel vm)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId)) return RedirectToAction("Login", "Account");
            
            var dto = new BeneficiaryTransactionDto 
            { 
                SourceAccountNumber = vm.SourceAccountNumber, 
                BeneficiaryId = vm.BeneficiaryId, 
                Amount = vm.Amount 
            };
            
            var result = await _transactionService.ProcessBeneficiaryTransactionAsync(dto, clientId);
            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction("Beneficiary");
            }
            TempData["SuccessMessage"] = "La transferencia al beneficiario ha sido procesada exitosamente.";
            return RedirectToAction("Index", "Customer");
        }

        [HttpGet]
        public async Task<IActionResult> PayCard()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId)) return RedirectToAction("Login", "Account");
            await PopulateSavingsAccountsAsync(clientId);
            await PopulateCreditCardsAsync(clientId);
            return View(new PayCardViewModel { SourceAccountNumber = "" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayCard(PayCardViewModel vm)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId)) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) { await PopulateSavingsAccountsAsync(clientId); await PopulateCreditCardsAsync(clientId); return View(vm); }

            var sourceResult = await _atmTransactionService.GetAtmAccountDetailsAsync(vm.SourceAccountNumber);
            if (!sourceResult.IsValid || sourceResult.Value!.Balance < vm.Amount)
            {
                ModelState.AddModelError(string.Empty, "La cuenta origen no es válida o tiene fondos insuficientes.");
                await PopulateSavingsAccountsAsync(clientId); await PopulateCreditCardsAsync(clientId); return View(vm);
            }

            var dashboardResult = await _dashboardService.GetClientDashboardAsync(clientId);
            var creditCard = dashboardResult.Value?.CreditCards.FirstOrDefault(c => c.Id == vm.CreditCardId);
            if (creditCard == null)
            {
                ModelState.AddModelError(string.Empty, "La tarjeta seleccionada no es válida.");
                await PopulateSavingsAccountsAsync(clientId); await PopulateCreditCardsAsync(clientId); return View(vm);
            }

            var confirmModel = new ConfirmPayCardViewModel
            {
                SourceAccountNumber = vm.SourceAccountNumber,
                OriginOwnerName = sourceResult.Value!.OwnerName,
                CreditCardId = vm.CreditCardId,
                CreditCardLastFour = creditCard.LastFourDigits,
                CreditCardOwnerName = sourceResult.Value!.OwnerName,
                Amount = vm.Amount
            };

            return View("ConfirmPayCard", confirmModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecutePayCard(ConfirmPayCardViewModel vm)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId)) return RedirectToAction("Login", "Account");
            
            var dto = new PayCreditCardDto 
            { 
                SourceAccountNumber = vm.SourceAccountNumber, 
                CreditCardId = vm.CreditCardId, 
                Amount = vm.Amount 
            };
            
            var result = await _paymentService.PayCreditCardAsync(dto, clientId);
            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction("PayCard");
            }
            TempData["SuccessMessage"] = "El pago de la tarjeta de crédito ha sido procesado exitosamente.";
            return RedirectToAction("Index", "Customer");
        }

        [HttpGet]
        public async Task<IActionResult> PayLoan()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId)) return RedirectToAction("Login", "Account");
            await PopulateSavingsAccountsAsync(clientId);
            await PopulateLoansAsync(clientId);
            return View(new PayLoanViewModel { SourceAccountNumber = "" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayLoan(PayLoanViewModel vm)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId)) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) { await PopulateSavingsAccountsAsync(clientId); await PopulateLoansAsync(clientId); return View(vm); }

            var sourceResult = await _atmTransactionService.GetAtmAccountDetailsAsync(vm.SourceAccountNumber);
            if (!sourceResult.IsValid || sourceResult.Value!.Balance < vm.Amount)
            {
                ModelState.AddModelError(string.Empty, "La cuenta origen no es válida o tiene fondos insuficientes.");
                await PopulateSavingsAccountsAsync(clientId); await PopulateLoansAsync(clientId); return View(vm);
            }

            var dashboardResult = await _dashboardService.GetClientDashboardAsync(clientId);
            var loan = dashboardResult.Value?.Loans.FirstOrDefault(l => l.Id == vm.LoanId);
            if (loan == null)
            {
                ModelState.AddModelError(string.Empty, "El préstamo seleccionado no es válido.");
                await PopulateSavingsAccountsAsync(clientId); await PopulateLoansAsync(clientId); return View(vm);
            }

            decimal effectiveAmount = vm.Amount > loan.PendientAmount ? loan.PendientAmount : vm.Amount;

            var confirmModel = new ConfirmPayLoanViewModel
            {
                SourceAccountNumber = vm.SourceAccountNumber,
                OriginOwnerName = sourceResult.Value!.OwnerName,
                LoanId = vm.LoanId,
                LoanNumber = "#" + loan.Id.ToString().PadLeft(8, '0'), // Mock loan number display
                LoanOwnerName = sourceResult.Value!.OwnerName,
                Amount = vm.Amount,
                EffectiveAmount = effectiveAmount
            };

            return View("ConfirmPayLoan", confirmModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecutePayLoan(ConfirmPayLoanViewModel vm)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(clientId)) return RedirectToAction("Login", "Account");
            
            var dto = new PayLoanDto 
            { 
                SourceAccountNumber = vm.SourceAccountNumber, 
                LoanId = vm.LoanId, 
                Amount = vm.Amount 
            };
            
            var result = await _paymentService.PayLoanAsync(dto, clientId);
            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction("PayLoan");
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
            var result = await _beneficiaryServices.GetClientBeneficiariesAsync(clientId);
            if (result.IsValid && result.Value != null)
            {
                ViewBag.Beneficiaries = _mapper.Map<List<BeneficiaryListViewModel>>(result.Value);
            }
            else
            {
                ViewBag.Beneficiaries = new List<BeneficiaryListViewModel>();
            }
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
