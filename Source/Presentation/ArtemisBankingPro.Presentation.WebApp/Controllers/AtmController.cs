using ArtemisBankingPro.Core.Application.ViewModels.Atm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers
{
    [Authorize(Roles = "Cajero")]
    public class AtmController : Controller
    {
        private readonly ILogger<AtmController> _logger;

        public AtmController(ILogger<AtmController> logger)
        {
            _logger = logger;
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
             * var loan = await _loanService.GetByNumberAsync(model.LoanNumber);
             * if (loan == null || loan.IsCompleted)
             * {
             *     ModelState.AddModelError("LoanNumber", "El número de préstamo ingresado no corresponde a un préstamo válido.");
             *     return View(model);
             * }
             * 
             * if (loan.PendingInstallments == 0)
             * {
             *     ModelState.AddModelError("LoanNumber", "El préstamo seleccionado no tiene cuotas pendientes de pago.");
             *     return View(model);
             * }
             * 
             * decimal effectiveAmount = Math.Min(model.Amount, loan.TotalDebt);
             * 
             * if (effectiveAmount > account.Balance)
             * {
             *     var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
             *     _logger.LogWarning("Intento de pago de préstamo rechazado por fondos insuficientes...");
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
            
            return RedirectToAction(nameof(ConfirmLoanPayment), new 
            { 
                sourceAccount = model.SourceAccountNumber, 
                loanNumber = model.LoanNumber,
                enteredAmount = model.Amount,
                effectiveAmount = mockEffectiveAmount
            });
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmLoanPayment(string sourceAccount, string loanNumber, decimal enteredAmount, decimal effectiveAmount)
        {
            var model = new ConfirmLoanPaymentViewModel
            {
                SourceAccountNumber = sourceAccount,
                AccountOwnerName = "John Doe (Mock)", 
                LoanOwnerName = "Jane Doe (Mock)", 
                LoanNumber = loanNumber,
                EnteredAmount = enteredAmount,
                EffectiveAmount = effectiveAmount
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteLoanPayment(ConfirmLoanPaymentViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            _logger.LogInformation("Iniciando proceso de pago a préstamo. Cajero: {UserId}, Cuenta Origen: {Cuenta}, Préstamo: {Prestamo}, Monto Efectivo: {Monto}", userId, model.SourceAccountNumber, model.LoanNumber, model.EffectiveAmount);
            
            /*
             * COMMENTED LOGIC WAITING FOR OTHER TEAMS' SERVICES
             * 
             * await _accountService.UpdateBalanceAsync(model.SourceAccountNumber, -model.EffectiveAmount);
             * await _loanService.ApplyAmortizedPaymentAsync(model.LoanNumber, model.EffectiveAmount);
             * 
             * var transaccion = new TransactionDto { Type = "DÉBITO", Origin = model.SourceAccountNumber, Beneficiary = model.LoanNumber, Status = "APROBADA", ... };
             * await _transactionService.RegisterTransactionAsync(transaccion);
             * 
             * try {
             *     var emailDto = new MessageDto { To = "loanowner@email.com", Subject = $"Pago realizado al préstamo {model.LoanNumber}", Body = "..." };
             *     await _emailService.SendNotification(emailDto);
             * } catch (Exception ex) {
             *     _logger.LogError(ex, "Error al enviar correo...");
             * }
             */

            TempData["SuccessMessage"] = "Pago de préstamo realizado correctamente (Mock).";
            return RedirectToAction("Index", "Home"); 
        }
    }
}
