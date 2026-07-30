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
        public IActionResult ThirdPartyTransaction()
        {
            return View(new ThirdPartyTransactionViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> ThirdPartyTransaction(ThirdPartyTransactionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.SourceAccountNumber == model.DestinationAccountNumber)
            {
                ModelState.AddModelError("DestinationAccountNumber", "La cuenta origen y la cuenta destino no pueden ser la misma.");
                return View(model);
            }

            /*
             * COMMENTED LOGIC WAITING FOR OTHER TEAMS' SERVICES
             * 
             * var sourceAccount = await _accountService.GetByNumberAsync(model.SourceAccountNumber);
             * if (sourceAccount == null || !sourceAccount.IsActive)
             * {
             *     ModelState.AddModelError("SourceAccountNumber", "El número de cuenta origen ingresado no corresponde a una cuenta válida.");
             *     return View(model);
             * }
             * 
             * var destinationAccount = await _accountService.GetByNumberAsync(model.DestinationAccountNumber);
             * if (destinationAccount == null || !destinationAccount.IsActive)
             * {
             *     ModelState.AddModelError("DestinationAccountNumber", "El número de cuenta destino ingresado no corresponde a una cuenta válida.");
             *     return View(model);
             * }
             * 
             * if (model.Amount > sourceAccount.Balance)
             * {
             *     var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
             *     _logger.LogWarning("Intento rechazado por fondos insuficientes...");
             *     
             *     var rejectedTransaction = new TransactionDto { Status = "RECHAZADO", Amount = model.Amount, Origin = model.SourceAccountNumber, Type = "DÉBITO" ... };
             *     await _transactionService.RegisterTransactionAsync(rejectedTransaction);
             *     
             *     ModelState.AddModelError("Amount", "El monto ingresado excede el saldo disponible de la cuenta.");
             *     return View(model);
             * }
             */

            return RedirectToAction(nameof(ConfirmThirdPartyTransaction), new 
            { 
                sourceAccount = model.SourceAccountNumber, 
                destinationAccount = model.DestinationAccountNumber,
                amount = model.Amount
            });
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmThirdPartyTransaction(string sourceAccount, string destinationAccount, decimal amount)
        {
            var model = new ConfirmThirdPartyTransactionViewModel
            {
                SourceAccountNumber = sourceAccount,
                SourceAccountOwnerName = "John Doe (Mock)", 
                DestinationAccountNumber = destinationAccount,
                DestinationAccountOwnerName = "Jane Doe (Mock)", 
                Amount = amount
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteThirdPartyTransaction(ConfirmThirdPartyTransactionViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            _logger.LogInformation("Iniciando transferencia a terceros. Cajero: {UserId}, Origen: {Origen}, Destino: {Destino}, Monto: {Monto}", userId, model.SourceAccountNumber, model.DestinationAccountNumber, model.Amount);
            
            /*
             * COMMENTED LOGIC WAITING FOR OTHER TEAMS' SERVICES
             * (This should ideally run inside a Database Transaction / Unit of Work)
             * 
             * await _accountService.UpdateBalanceAsync(model.SourceAccountNumber, -model.Amount);
             * await _accountService.UpdateBalanceAsync(model.DestinationAccountNumber, model.Amount);
             * 
             * var debitTransaction = new TransactionDto { Type = "DÉBITO", Origin = model.SourceAccountNumber, Beneficiary = model.DestinationAccountNumber, Status = "APROBADA", ... };
             * await _transactionService.RegisterTransactionAsync(debitTransaction);
             * 
             * var creditTransaction = new TransactionDto { Type = "CRÉDITO", Origin = model.SourceAccountNumber, Beneficiary = model.DestinationAccountNumber, Status = "APROBADA", ... };
             * await _transactionService.RegisterTransactionAsync(creditTransaction);
             * 
             * try {
             *     var emailSenderDto = new MessageDto { To = "sender@email.com", Subject = $"Transacción realizada a la cuenta {model.DestinationAccountNumber.Substring(Math.Max(0, model.DestinationAccountNumber.Length - 4))}", Body = "..." };
             *     await _emailService.SendNotification(emailSenderDto);
             *     
             *     var emailReceiverDto = new MessageDto { To = "receiver@email.com", Subject = $"Transacción enviada desde la cuenta {model.SourceAccountNumber.Substring(Math.Max(0, model.SourceAccountNumber.Length - 4))}", Body = "..." };
             *     await _emailService.SendNotification(emailReceiverDto);
             * } catch (Exception ex) {
             *     _logger.LogError(ex, "Error al enviar correos...");
             * }
             */

            TempData["SuccessMessage"] = "Transacción a terceros realizada correctamente (Mock).";
            return RedirectToAction("Index", "Home"); 
        }
    }
}
