using ArtemisBankingPro.Core.Application.ViewModels.Atm;
using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
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
        private readonly IAtmTransactionService _transactionService;
        // private readonly IEmailServices _emailService;

        public AtmController(
            ILogger<AtmController> logger,
            IAtmTransactionService transactionService
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
            var result = await _transactionService.GetAtmCashierDailyIndicatorsAsync(userId);
            
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

            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "El monto a pagar debe ser mayor que cero.");
                return View(model);
            }

            var accountResult = await _transactionService.GetAtmAccountDetailsAsync(model.SourceAccountNumber);
            if (!accountResult.IsValid || !accountResult.Value!.IsActive)
            {
                ModelState.AddModelError("SourceAccountNumber", "El número de cuenta ingresado no corresponde a una cuenta válida.");
                return View(model);
            }

            var cardResult = await _transactionService.GetAtmCreditCardDetailsAsync(model.CreditCardNumber);
            if (!cardResult.IsValid || !cardResult.Value!.IsActive)
            {
                ModelState.AddModelError("CreditCardNumber", "El número de tarjeta ingresado no corresponde a una tarjeta válida.");
                return View(model);
            }

            if (cardResult.Value!.Debt <= 0)
            {
                ModelState.AddModelError("CreditCardNumber", "La tarjeta seleccionada no tiene deuda pendiente.");
                return View(model);
            }

            decimal effectiveAmount = Math.Min(model.Amount, cardResult.Value.Debt);

            if (accountResult.Value!.Balance < effectiveAmount)
            {
                ModelState.AddModelError("Amount", "El monto ingresado excede el saldo disponible de la cuenta.");
                return View(model);
            }
            
            var confirmModel = new ConfirmCreditCardPaymentViewModel
            {
                SourceAccountNumber = model.SourceAccountNumber,
                AccountOwnerName = accountResult.Value.OwnerName,
                CreditCardNumber = model.CreditCardNumber,
                CreditCardOwnerName = cardResult.Value.OwnerName,
                CardLastFourDigits = model.CreditCardNumber.Substring(model.CreditCardNumber.Length - 4),
                EnteredAmount = model.Amount,
                EffectiveAmount = effectiveAmount
            };

            // Pasamos el modelo directamente a la vista de confirmación
            return View("ConfirmCreditCardPayment", confirmModel);
        }

        [HttpGet]
        public IActionResult ConfirmCreditCardPayment()
        {
            // Este método GET solo existe por si el usuario recarga la página, lo devolvemos al inicio.
            return RedirectToAction(nameof(CreditCardPayment));
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteCreditCardPayment(ConfirmCreditCardPaymentViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            
            _logger.LogInformation("Iniciando proceso de pago a tarjeta. Cajero: {UserId}, Cuenta Origen: {Cuenta}, Monto Efectivo: {Monto}", userId, model.SourceAccountNumber, model.EffectiveAmount);
            
            var result = await _transactionService.ProcessAtmCreditCardPaymentAsync(new Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmCreditCardPaymentDto 
            { 
                SourceAccountNumber = model.SourceAccountNumber, 
                CreditCardNumber = model.CreditCardNumber, 
                Amount = model.EffectiveAmount, 
                CashierId = userId 
            });

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

            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "El monto a pagar debe ser mayor que cero.");
                return View(model);
            }

            var accountResult = await _transactionService.GetAtmAccountDetailsAsync(model.OriginAccountNumber);
            if (!accountResult.IsValid || !accountResult.Value!.IsActive)
            {
                ModelState.AddModelError("OriginAccountNumber", "El número de cuenta ingresado no corresponde a una cuenta válida.");
                return View(model);
            }

            var loanResult = await _transactionService.GetAtmLoanDetailsAsync(model.LoanNumber);
            if (!loanResult.IsValid || !loanResult.Value!.IsActive)
            {
                ModelState.AddModelError("LoanNumber", "El número de préstamo ingresado no corresponde a un préstamo válido.");
                return View(model);
            }

            if (!loanResult.Value.HasPendingInstallments || loanResult.Value.PendingAmount <= 0)
            {
                ModelState.AddModelError("LoanNumber", "El préstamo seleccionado no tiene cuotas pendientes de pago.");
                return View(model);
            }

            decimal effectiveAmount = Math.Min(model.Amount, loanResult.Value.PendingAmount);

            if (accountResult.Value!.Balance < effectiveAmount)
            {
                ModelState.AddModelError("Amount", "El monto ingresado excede el saldo disponible de la cuenta.");
                return View(model);
            }

            var confirmModel = new LoanPaymentConfirmationViewModel
            {
                OriginAccountNumber = model.OriginAccountNumber,
                OriginAccountHolderName = accountResult.Value.OwnerName,
                LoanNumber = model.LoanNumber,
                LoanHolderName = loanResult.Value.OwnerName,
                Amount = model.Amount,
                EffectiveAmount = effectiveAmount
            };

            return View("ConfirmLoanPayment", confirmModel);
        }

        [HttpGet]
        public IActionResult ConfirmLoanPayment()
        {
            return RedirectToAction(nameof(LoanPayment));
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteLoanPayment(LoanPaymentConfirmationViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            var result = await _transactionService.ProcessAtmLoanPaymentAsync(new Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmLoanPaymentDto
            {
                SourceAccountNumber = model.OriginAccountNumber,
                LoanNumber = model.LoanNumber,
                Amount = model.EffectiveAmount,
                CashierId = userId
            });

            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = result.Errors.FirstOrDefault()?.Description ?? "Error al procesar el pago de préstamo.";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "Pago de préstamo realizado correctamente.";
            return RedirectToAction("Index");
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

            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "El monto de la transacción debe ser mayor que cero.");
                return View(model);
            }

            if (model.OriginAccountNumber == model.DestinationAccountNumber)
            {
                ModelState.AddModelError("DestinationAccountNumber", "La cuenta origen y la cuenta destino no pueden ser la misma.");
                return View(model);
            }

            var originResult = await _transactionService.GetAtmAccountDetailsAsync(model.OriginAccountNumber);
            if (!originResult.IsValid || !originResult.Value!.IsActive)
            {
                ModelState.AddModelError("OriginAccountNumber", "El número de cuenta origen ingresado no corresponde a una cuenta válida.");
                return View(model);
            }

            var destResult = await _transactionService.GetAtmAccountDetailsAsync(model.DestinationAccountNumber);
            if (!destResult.IsValid || !destResult.Value!.IsActive)
            {
                ModelState.AddModelError("DestinationAccountNumber", "El número de cuenta destino ingresado no corresponde a una cuenta válida.");
                return View(model);
            }

            if (originResult.Value!.Balance < model.Amount)
            {
                ModelState.AddModelError("Amount", "El monto ingresado excede el saldo disponible de la cuenta.");
                return View(model);
            }

            var confirmModel = new ThirdPartyTransferConfirmationViewModel
            {
                OriginAccountNumber = model.OriginAccountNumber,
                DestinationAccountNumber = model.DestinationAccountNumber,
                Amount = model.Amount,
                OriginAccountHolderName = originResult.Value.OwnerName,
                DestinationAccountHolderName = destResult.Value.OwnerName
            };

            return View("ConfirmThirdPartyTransfer", confirmModel);
        }

        [HttpGet]
        public IActionResult ConfirmThirdPartyTransfer()
        {
            return RedirectToAction(nameof(ThirdPartyTransfer));
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteThirdPartyTransfer(ThirdPartyTransferConfirmationViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            var result = await _transactionService.ProcessAtmThirdPartyTransferAsync(new Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmThirdPartyTransferDto
            {
                SourceAccountNumber = model.OriginAccountNumber,
                DestinationAccountNumber = model.DestinationAccountNumber,
                Amount = model.Amount,
                CashierId = userId
            });

            if (!result.IsValid)
            {
                TempData["ErrorMessage"] = result.Errors.FirstOrDefault()?.Description ?? "Error al procesar la transferencia.";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "Transferencia realizada correctamente.";
            return RedirectToAction("Index");
        }
    }
}

