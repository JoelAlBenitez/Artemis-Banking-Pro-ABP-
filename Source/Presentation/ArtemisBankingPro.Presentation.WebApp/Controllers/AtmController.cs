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
        // private readonly IEmailServices _emailService;
        // private readonly IAccountService _accountService; // Handled by Admin team
        // private readonly ICreditCardService _creditCardService; // Handled by Admin team
        // private readonly ITransactionService _transactionService; // Handled by Client team

        public AtmController(
            ILogger<AtmController> logger
            // IEmailServices emailService
            )
        {
            _logger = logger;
            // _emailService = emailService;
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            _logger.LogInformation("Iniciando proceso de pago a tarjeta. Cajero: {UserId}, Cuenta Origen: {Cuenta}, Monto Efectivo: {Monto}", userId, model.SourceAccountNumber, model.EffectiveAmount);
            
            /*
             * COMMENTED LOGIC WAITING FOR OTHER TEAMS' SERVICES
             * 
             * await _accountService.UpdateBalanceAsync(model.SourceAccountNumber, -model.EffectiveAmount);
             * await _creditCardService.ApplyPaymentAsync(model.CardLastFourDigits, model.EffectiveAmount);
             * 
             * var transaccion = new TransactionDto { Type = "DÉBITO", Origin = model.SourceAccountNumber, Beneficiary = model.CardLastFourDigits, Status = "APROBADA", ... };
             * await _transactionService.RegisterTransactionAsync(transaccion);
             * 
             * _logger.LogInformation("Pago procesado exitosamente...");
             * 
             * try {
             *     var emailDto = new MessageDto { To = "cardowner@email.com", Subject = $"Pago realizado a la tarjeta {model.CardLastFourDigits}", Body = "..." };
             *     await _emailService.SendNotification(emailDto);
             * } catch (Exception ex) {
             *     _logger.LogError(ex, "Error al enviar correo...");
             * }
             */

            TempData["SuccessMessage"] = "Pago de tarjeta realizado correctamente (Mock).";
            return RedirectToAction("Index", "Home"); 
        }
    }
}
