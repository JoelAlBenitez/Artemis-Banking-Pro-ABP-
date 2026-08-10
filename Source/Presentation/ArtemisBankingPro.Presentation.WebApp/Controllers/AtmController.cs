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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // TODO: Process payment with ITransactionServices or ILoanServices (handle amortization logic)
            // bool success = await _transactionServices.ProcessLoanPaymentAsync(model, userId);
            bool success = true; // Placeholder

            if (!success)
            {
                ModelState.AddModelError("", "An error occurred while processing the payment.");
                return View(model);
            }

            // TODO: Get actual emails from services when available
            var loanHolderEmail = "loanholder@example.com";
            var accountHolderEmail = "accountholder@example.com";

            var accountSuffix = model.OriginAccountNumber.Length >= 4 
                ? model.OriginAccountNumber.Substring(model.OriginAccountNumber.Length - 4) 
                : model.OriginAccountNumber;

            // Email to Loan Holder
            var loanHolderMessage = new MessageDto
            {
                To = loanHolderEmail,
                Subject = $"Pago realizado al préstamo {model.LoanNumber}",
                Message = $"Hola {model.LoanHolderName},\n\nSe ha realizado un pago a su préstamo {model.LoanNumber}.\nMonto pagado: RD${model.EffectiveAmount}\nCuenta origen terminada en: {accountSuffix}\nFecha y hora: {System.DateTime.Now.ToString("g")}\n\nSi usted no reconoce esta operación, comuníquese con la entidad bancaria."
            };

            var emailSentToLoanHolder = await _emailServices.SendNotification(loanHolderMessage);
            bool emailSentToAccountHolder = true;

            // Rule: If owners are different, send email to account owner too
            if (model.OriginAccountHolderName != model.LoanHolderName)
            {
                var accountHolderMessage = new MessageDto
                {
                    To = accountHolderEmail,
                    Subject = $"Débito por pago a préstamo {model.LoanNumber}",
                    Message = $"Hola {model.OriginAccountHolderName},\n\nSe ha debitado dinero de su cuenta terminada en {accountSuffix} para realizar un pago al préstamo {model.LoanNumber}.\nMonto debitado: RD${model.EffectiveAmount}\nFecha y hora: {System.DateTime.Now.ToString("g")}\n\nSi usted no reconoce esta operación, comuníquese con la entidad bancaria."
                };
                emailSentToAccountHolder = await _emailServices.SendNotification(accountHolderMessage);
            }

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // TODO: Process payment with ITransactionServices or ILoanServices (handle amortization logic)
            // bool success = await _transactionServices.ProcessLoanPaymentAsync(model, userId);
            bool success = true; // Placeholder

            if (!success)
            {
                ModelState.AddModelError("", "An error occurred while processing the payment.");
                return View(model);
            }

            // TODO: Get actual emails from services when available
            var loanHolderEmail = "loanholder@example.com";
            var accountHolderEmail = "accountholder@example.com";

            var accountSuffix = model.OriginAccountNumber.Length >= 4 
                ? model.OriginAccountNumber.Substring(model.OriginAccountNumber.Length - 4) 
                : model.OriginAccountNumber;

            // Email to Loan Holder
            var loanHolderMessage = new MessageDto
            {
                To = loanHolderEmail,
                Subject = $"Pago realizado al préstamo {model.LoanNumber}",
                Message = $"Hola {model.LoanHolderName},\n\nSe ha realizado un pago a su préstamo {model.LoanNumber}.\nMonto pagado: RD${model.EffectiveAmount}\nCuenta origen terminada en: {accountSuffix}\nFecha y hora: {System.DateTime.Now.ToString("g")}\n\nSi usted no reconoce esta operación, comuníquese con la entidad bancaria."
            };

            var emailSentToLoanHolder = await _emailServices.SendNotification(loanHolderMessage);
            bool emailSentToAccountHolder = true;

            // Rule: If owners are different, send email to account owner too
            if (model.OriginAccountHolderName != model.LoanHolderName)
            {
                var accountHolderMessage = new MessageDto
                {
                    To = accountHolderEmail,
                    Subject = $"Débito por pago a préstamo {model.LoanNumber}",
                    Message = $"Hola {model.OriginAccountHolderName},\n\nSe ha debitado dinero de su cuenta terminada en {accountSuffix} para realizar un pago al préstamo {model.LoanNumber}.\nMonto debitado: RD${model.EffectiveAmount}\nFecha y hora: {System.DateTime.Now.ToString("g")}\n\nSi usted no reconoce esta operación, comuníquese con la entidad bancaria."
                };
                emailSentToAccountHolder = await _emailServices.SendNotification(accountHolderMessage);
            }

            if (!emailSentToLoanHolder || !emailSentToAccountHolder)
            {
                TempData["WarningMessage"] = "The payment was completed successfully, but the notification email could not be sent.";
            }

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // TODO: Process transfer with ITransactionServices (Atomically debit and credit)
            // bool success = await _transactionServices.ProcessThirdPartyTransferAsync(model, userId);
            bool success = true; // Placeholder

            if (!success)
            {
                ModelState.AddModelError("", "An error occurred while processing the transfer.");
                return View(model);
            }

            // TODO: Get actual emails from services when available
            var originHolderEmail = "origin@example.com";
            var destinationHolderEmail = "destination@example.com";

            var originSuffix = model.OriginAccountNumber.Length >= 4 
                ? model.OriginAccountNumber.Substring(model.OriginAccountNumber.Length - 4) 
                : model.OriginAccountNumber;
                
            var destinationSuffix = model.DestinationAccountNumber.Length >= 4 
                ? model.DestinationAccountNumber.Substring(model.DestinationAccountNumber.Length - 4) 
                : model.DestinationAccountNumber;

            // Email to Origin Holder
            var originHolderMessage = new MessageDto
            {
                To = originHolderEmail,
                Subject = $"Transacción realizada a la cuenta {destinationSuffix}",
                Message = $"Hola {model.OriginAccountHolderName},\n\nSe ha realizado un envío de dinero hacia otra cuenta.\nMonto transferido: RD${model.Amount}\nCuenta origen terminada en: {originSuffix}\nCuenta destino terminada en: {destinationSuffix}\nFecha y hora: {System.DateTime.Now.ToString("g")}\n\nSi usted no reconoce esta operación, comuníquese con la entidad bancaria."
            };

            var emailSentToOrigin = await _emailServices.SendNotification(originHolderMessage);

            // Email to Destination Holder
            var destinationHolderMessage = new MessageDto
            {
                To = destinationHolderEmail,
                Subject = $"Transacción enviada desde la cuenta {originSuffix}",
                Message = $"Hola {model.DestinationAccountHolderName},\n\nHa recibido una transacción desde otra cuenta.\nMonto recibido: RD${model.Amount}\nCuenta origen terminada en: {originSuffix}\nCuenta destino terminada en: {destinationSuffix}\nFecha y hora: {System.DateTime.Now.ToString("g")}\n\nSi usted no reconoce esta operación, comuníquese con la entidad bancaria."
            };

            var emailSentToDestination = await _emailServices.SendNotification(destinationHolderMessage);

            if (!emailSentToOrigin || !emailSentToDestination)
            {
                TempData["WarningMessage"] = "The transaction was completed successfully, but one or more notification emails could not be sent.";
            }

            return RedirectToAction("Index");
        }
    }
}
