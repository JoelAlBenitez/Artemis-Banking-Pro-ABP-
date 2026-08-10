using ArtemisBankingPro.Core.Application.ViewModels.Atm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

// We will inject the EmailService from Common, but omit Account and Card services to avoid dependency issues with other teams.
// using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
// using Artemis_Banking_Pro.Core.Application.DTOs.Messages;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers
{
    [Authorize(Roles = "Cajero")]
    public class AtmController : Controller
    {
        private readonly ILogger<AtmController> _logger;
        private readonly Artemis_Banking_Pro.Core.Application.Contracts.Transactions.ITransactionService _transactionService;
        // private readonly IEmailServices _emailService;

        public AtmController(
            ILogger<AtmController> logger,
            Artemis_Banking_Pro.Core.Application.Contracts.Transactions.ITransactionService transactionService
            // IEmailServices emailService
            )
        {
            _logger = logger;
            _transactionService = transactionService;
            // _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            
            var result = await _transactionService.GetCashierDailyIndicatorsAsync(userId);
            
            ViewBag.TotalTransactions = result.IsValid && result.Value != null ? result.Value.TotalTransactions : 0;
            ViewBag.TotalPayments = result.IsValid && result.Value != null ? result.Value.TotalPayments : 0;
            ViewBag.TotalDeposits = result.IsValid && result.Value != null ? result.Value.TotalDeposits : 0;
            ViewBag.TotalWithdrawals = result.IsValid && result.Value != null ? result.Value.TotalWithdrawals : 0;
            
            return View();
        }

        [HttpGet]
        public IActionResult Deposit()
        {
            return View(new DepositViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Deposit(DepositViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "El monto a depositar debe ser mayor que cero.");
                return View(model);
            }

            var accountResult = await _transactionService.GetAtmAccountDetailsAsync(model.DestinationAccountNumber);
            if (!accountResult.IsValid)
            {
                ModelState.AddModelError("DestinationAccountNumber", "El número de cuenta ingresado no corresponde a una cuenta válida.");
                return View(model);
            }

            var confirmModel = new ConfirmDepositViewModel
            {
                DestinationAccountNumber = model.DestinationAccountNumber,
                AccountOwnerName = accountResult.Value!.OwnerName,
                Amount = model.Amount
            };

            return View("ConfirmDeposit", confirmModel);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmDeposit(ConfirmDepositViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            _logger.LogInformation("Iniciando depósito. Cajero: {UserId}, Cuenta Destino: {Cuenta}, Monto: {Monto}", userId, model.DestinationAccountNumber, model.Amount);

            var depositDto = new Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmDepositDto { DestinationAccountNumber = model.DestinationAccountNumber, Amount = model.Amount, CashierId = userId };
            var result = await _transactionService.ProcessAtmDepositAsync(depositDto);

            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = result.Errors.FirstOrDefault()?.Description ?? "Error al procesar el depósito";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Depósito realizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Withdrawal()
        {
            return View(new WithdrawalViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Withdrawal(WithdrawalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "El monto a retirar debe ser mayor que cero.");
                return View(model);
            }

            var accountResult = await _transactionService.GetAtmAccountDetailsAsync(model.OriginAccountNumber);
            if (!accountResult.IsValid)
            {
                ModelState.AddModelError("OriginAccountNumber", "El número de cuenta ingresado no corresponde a una cuenta válida.");
                return View(model);
            }

            if (accountResult.Value!.Balance < model.Amount)
            {
                ModelState.AddModelError("Amount", "El monto ingresado excede el saldo disponible de la cuenta.");
                return View(model);
            }

            var confirmModel = new ConfirmWithdrawalViewModel
            {
                OriginAccountNumber = model.OriginAccountNumber,
                AccountOwnerName = accountResult.Value.OwnerName,
                Amount = model.Amount
            };

            return View("ConfirmWithdrawal", confirmModel);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmWithdrawal(ConfirmWithdrawalViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            _logger.LogInformation("Iniciando retiro. Cajero: {UserId}, Cuenta Origen: {Cuenta}, Monto: {Monto}", userId, model.OriginAccountNumber, model.Amount);

            var result = await _transactionService.ProcessAtmWithdrawalAsync(new Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmWithdrawalDto { SourceAccountNumber = model.OriginAccountNumber, Amount = model.Amount, CashierId = userId });
            
            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = result.Errors.FirstOrDefault()?.Description ?? "Error al procesar el retiro";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Retiro realizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult CreditCardPayment()
        {
            return View(new CreditCardPaymentViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CreditCardPayment(CreditCardPaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            /*
             * COMMENTED LOGIC WAITING FOR OTHER TEAMS' SERVICES
             * 
             * var account = await _accountService.GetByNumberAsync(model.SourceAccountNumber);
             * if (account == null || !account.IsActive)
             * {
             *     ModelState.AddModelError("SourceAccountNumber", "El número de cuenta ingresado no corresponde a una cuenta válida.");
             *     return View(model);
             * }
             * 
             * var card = await _creditCardService.GetByNumberAsync(model.CreditCardNumber);
             * if (card == null || !card.IsActive)
             * {
             *     ModelState.AddModelError("CreditCardNumber", "El número de tarjeta ingresado no corresponde a una tarjeta válida.");
             *     return View(model);
             * }
             * 
             * if (card.Debt <= 0)
             * {
             *     ModelState.AddModelError("CreditCardNumber", "La tarjeta seleccionada no tiene deuda pendiente.");
             *     return View(model);
             * }
             * 
             * decimal effectiveAmount = Math.Min(model.Amount, card.Debt);
             * 
             * if (effectiveAmount > account.Balance)
             * {
             *     var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
             *     _logger.LogWarning("Intento rechazado por fondos insuficientes. Cajero: {UserId}, Cuenta Origen: {Cuenta}, Monto efectivo: {Monto}", userId, model.SourceAccountNumber, effectiveAmount);
             *     
             *     var rejectedTransaction = new TransactionDto { Status = "RECHAZADO", Amount = effectiveAmount, Origin = model.SourceAccountNumber, Type = "DÉBITO" ... };
             *     await _transactionService.RegisterTransactionAsync(rejectedTransaction);
             *     
             *     ModelState.AddModelError("Amount", "El monto ingresado excede el saldo disponible de la cuenta.");
             *     return View(model);
             * }
             */

            // TEMPORARY MOCK FOR UI TESTING
            decimal mockEffectiveAmount = model.Amount; 
            
            return RedirectToAction(nameof(ConfirmCreditCardPayment), new 
            { 
                sourceAccount = model.SourceAccountNumber, 
                cardLastFour = model.CreditCardNumber.Substring(model.CreditCardNumber.Length - 4),
                enteredAmount = model.Amount,
                effectiveAmount = mockEffectiveAmount
            });
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmCreditCardPayment(string sourceAccount, string cardLastFour, decimal enteredAmount, decimal effectiveAmount)
        {
            // var account = await _accountService.GetByNumberAsync(sourceAccount);
            // var card = await _creditCardService.GetCardByLastFourAsync(cardLastFour);

            var model = new ConfirmCreditCardPaymentViewModel
            {
                SourceAccountNumber = sourceAccount,
                AccountOwnerName = "John Doe (Mock)", // account.OwnerName
                CreditCardOwnerName = "Jane Doe (Mock)", // card.OwnerName
                CardLastFourDigits = cardLastFour,
                EnteredAmount = enteredAmount,
                EffectiveAmount = effectiveAmount
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteCreditCardPayment(ConfirmCreditCardPaymentViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            
            _logger.LogInformation("Iniciando proceso de pago a tarjeta. Cajero: {UserId}, Cuenta Origen: {Cuenta}, Monto Efectivo: {Monto}", userId, model.SourceAccountNumber, model.EffectiveAmount);
            
            var result = await _transactionService.ProcessAtmCreditCardPaymentAsync(new Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmCreditCardPaymentDto { SourceAccountNumber = model.SourceAccountNumber, CreditCardNumber = model.CardLastFourDigits, Amount = model.EffectiveAmount, CashierId = userId });

            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = result.Errors.FirstOrDefault()?.Description ?? "Error al procesar el pago de tarjeta";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "Pago de tarjeta realizado correctamente.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult LoanPayment()
        {
            return View(new LoanPaymentViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> LoanPayment(LoanPaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // TODO: Validate account with IAccountServices
            // bool isAccountActive = await _accountServices.IsAccountActiveAsync(model.OriginAccountNumber);
            bool isAccountActive = true; // Placeholder

            if (!isAccountActive)
            {
                ModelState.AddModelError("OriginAccountNumber", "The account number entered does not correspond to a valid account.");
                return View(model);
            }

            // TODO: Validate loan with ILoanServices
            // bool isLoanActive = await _loanServices.IsLoanActiveAsync(model.LoanNumber);
            bool isLoanActive = true; // Placeholder

            if (!isLoanActive)
            {
                ModelState.AddModelError("LoanNumber", "The loan number entered does not correspond to a valid loan.");
                return View(model);
            }

            // TODO: Validate loan has pending installments
            // bool hasPendingInstallments = await _loanServices.HasPendingInstallmentsAsync(model.LoanNumber);
            bool hasPendingInstallments = true; // Placeholder

            if (!hasPendingInstallments)
            {
                ModelState.AddModelError("LoanNumber", "The selected loan has no pending installments.");
                return View(model);
            }

            // TODO: Validate total pending debt
            // decimal totalPendingDebt = await _loanServices.GetTotalPendingDebtAsync(model.LoanNumber);
            decimal totalPendingDebt = 2000.00m; // Placeholder debt

            // Rule: Effective amount cannot exceed total pending debt
            decimal effectiveAmount = System.Math.Min(model.Amount, totalPendingDebt);

            // TODO: Validate sufficient balance with IAccountServices
            // bool hasSufficientBalance = await _accountServices.HasSufficientBalanceAsync(model.OriginAccountNumber, effectiveAmount);
            bool hasSufficientBalance = true; // Placeholder

            if (!hasSufficientBalance)
            {
                ModelState.AddModelError("Amount", "The entered amount exceeds the available balance of the account.");
                return View(model);
            }

            // TODO: Get account holder names
            // var originAccountHolder = await _accountServices.GetAccountHolderNameAsync(model.OriginAccountNumber);
            // var loanHolder = await _loanServices.GetLoanHolderNameAsync(model.LoanNumber);
            var originAccountHolder = "Origin Placeholder Name"; 
            var loanHolder = "Loan Placeholder Name";

            var confirmationModel = new LoanPaymentConfirmationViewModel
            {
                OriginAccountNumber = model.OriginAccountNumber,
                LoanNumber = model.LoanNumber,
                Amount = model.Amount,
                EffectiveAmount = effectiveAmount,
                OriginAccountHolderName = originAccountHolder,
                LoanHolderName = loanHolder
            };

            return View("ConfirmLoanPayment", confirmationModel);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmLoanPayment(LoanPaymentConfirmationViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            _logger.LogInformation("Iniciando pago a préstamo. Cajero: {UserId}, Préstamo: {Prestamo}, Monto Efectivo: {Monto}", userId, model.LoanNumber, model.EffectiveAmount);

            var result = await _transactionService.ProcessAtmLoanPaymentAsync(new Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmLoanPaymentDto { SourceAccountNumber = model.OriginAccountNumber, LoanNumber = model.LoanNumber, Amount = model.EffectiveAmount, CashierId = userId });
            
            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = result.Errors.FirstOrDefault()?.Description ?? "Error al procesar el pago al préstamo";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Pago a préstamo realizado correctamente.";
            return RedirectToAction(nameof(Index));
        }



        [HttpGet]
        public IActionResult ThirdPartyTransfer()
        {
            return View(new ThirdPartyTransferViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> ThirdPartyTransfer(ThirdPartyTransferViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Rule: Origin and Destination cannot be the same
            if (model.OriginAccountNumber == model.DestinationAccountNumber)
            {
                ModelState.AddModelError("DestinationAccountNumber", "The origin account and the destination account cannot be the same.");
                return View(model);
            }

            // TODO: Validate origin account with IAccountServices
            // bool isOriginActive = await _accountServices.IsAccountActiveAsync(model.OriginAccountNumber);
            bool isOriginActive = true; // Placeholder

            if (!isOriginActive)
            {
                ModelState.AddModelError("OriginAccountNumber", "The origin account number entered does not correspond to a valid account.");
                return View(model);
            }

            // TODO: Validate destination account with IAccountServices
            // bool isDestinationActive = await _accountServices.IsAccountActiveAsync(model.DestinationAccountNumber);
            bool isDestinationActive = true; // Placeholder

            if (!isDestinationActive)
            {
                ModelState.AddModelError("DestinationAccountNumber", "The destination account number entered does not correspond to a valid account.");
                return View(model);
            }

            // TODO: Validate sufficient balance with IAccountServices
            // bool hasSufficientBalance = await _accountServices.HasSufficientBalanceAsync(model.OriginAccountNumber, model.Amount);
            bool hasSufficientBalance = true; // Placeholder

            if (!hasSufficientBalance)
            {
                ModelState.AddModelError("Amount", "The entered amount exceeds the available balance of the account.");
                return View(model);
            }

            // TODO: Get account holder names
            // var originAccountHolder = await _accountServices.GetAccountHolderNameAsync(model.OriginAccountNumber);
            // var destinationAccountHolder = await _accountServices.GetAccountHolderNameAsync(model.DestinationAccountNumber);
            var originAccountHolder = "Origin Placeholder Name"; 
            var destinationAccountHolder = "Destination Placeholder Name";

            var confirmationModel = new ThirdPartyTransferConfirmationViewModel
            {
                OriginAccountNumber = model.OriginAccountNumber,
                DestinationAccountNumber = model.DestinationAccountNumber,
                Amount = model.Amount,
                OriginAccountHolderName = originAccountHolder,
                DestinationAccountHolderName = destinationAccountHolder
            };

            return View("ConfirmThirdPartyTransfer", confirmationModel);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmThirdPartyTransfer(ThirdPartyTransferConfirmationViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            
            var result = await _transactionService.ProcessAtmThirdPartyTransferAsync(new Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmThirdPartyTransferDto { SourceAccountNumber = model.OriginAccountNumber, DestinationAccountNumber = model.DestinationAccountNumber, Amount = model.Amount, CashierId = userId });
            
            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = result.Errors.FirstOrDefault()?.Description ?? "Error al procesar la transferencia a terceros";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Transferencia a terceros realizada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}

