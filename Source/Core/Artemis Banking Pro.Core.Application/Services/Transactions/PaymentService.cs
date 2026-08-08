using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.Transactions
{
    public sealed class PaymentService : IPaymentService
    {
        private readonly ISavingsAccountsRepository _savingsAccountRepository;
        private readonly ICreditCardsRepository _creditCardRepository;
        private readonly ILoansRepository _loansRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICardConsumptionRepository _cardConsumptionRepository;
        private readonly ICardPaymentRepository _cardPaymentRepository;
        private readonly ILoanInstallmentRepository _loanInstallmentRepository;
        private readonly ILoansPaymentRepository _loansPaymentRepository;
        private readonly ITransactionsValidationServices _validationServices;
        private readonly IEmailServices _emailServices;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            ISavingsAccountsRepository savingsAccountRepository,
            ICreditCardsRepository creditCardRepository,
            ILoansRepository loansRepository,
            ITransactionRepository transactionRepository,
            ICardConsumptionRepository cardConsumptionRepository,
            ICardPaymentRepository cardPaymentRepository,
            ILoanInstallmentRepository loanInstallmentRepository,
            ILoansPaymentRepository loansPaymentRepository,
            ITransactionsValidationServices validationServices,
            IEmailServices emailServices,
            ILogger<PaymentService> logger)
        {
            _savingsAccountRepository = savingsAccountRepository;
            _creditCardRepository = creditCardRepository;
            _loansRepository = loansRepository;
            _transactionRepository = transactionRepository;
            _cardConsumptionRepository = cardConsumptionRepository;
            _cardPaymentRepository = cardPaymentRepository;
            _loanInstallmentRepository = loanInstallmentRepository;
            _loansPaymentRepository = loansPaymentRepository;
            _validationServices = validationServices;
            _emailServices = emailServices;
            _logger = logger;
        }

        public async Task<ValidationResult<TransactionResultDto>> PayCreditCardAsync(PayCreditCardDto dto, string clientId)
        {
            _logger.LogInformation("Iniciando procesamiento de pago de tarjeta de crédito ID {CardId} para el cliente {ClientId} por RD${Amount}", dto.CreditCardId, clientId, dto.Amount);

            var validation = await _validationServices.ValidateCreditCardPaymentAsync(dto, clientId);
            if (!validation.IsValid)
            {
                return ValidationResult<TransactionResultDto>.Failure(validation.Errors.ToList());
            }

            try
            {
                var (originAccount, creditCard, effectiveAmount) = validation.Value;
                var result = await ExecuteCreditCardPaymentAsync(originAccount, creditCard, dto.Amount, effectiveAmount, clientId);
                if (!result.IsValid)
                {
                    return result;
                }

                _logger.LogInformation("Pago de tarjeta de crédito ID {CardId} procesado exitosamente por monto efectivo RD${EffectiveAmount}", dto.CreditCardId, effectiveAmount);
                
                var emailSent = await SendCreditCardPaymentEmailAsync(originAccount, creditCard, effectiveAmount, clientId);
                if (!emailSent)
                {
                    result.Value!.WarningMessage = "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico inesperado al procesar el pago de tarjeta para el cliente {ClientId}", clientId);
                return ValidationResult<TransactionResultDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<TransactionResultDto>> PayLoanAsync(PayLoanDto dto, string clientId)
        {
            _logger.LogInformation("Iniciando procesamiento de pago de préstamo ID {LoanId} para el cliente {ClientId} por RD${Amount}", dto.LoanId, clientId, dto.Amount);

            var validation = await _validationServices.ValidateLoanPaymentAsync(dto, clientId);
            if (!validation.IsValid)
            {
                return ValidationResult<TransactionResultDto>.Failure(validation.Errors.ToList());
            }

            try
            {
                var (originAccount, loan, installments, effectiveAmount) = validation.Value;
                var result = await ExecuteLoanPaymentAsync(originAccount, loan, installments, dto.Amount, effectiveAmount, clientId);
                if (!result.IsValid)
                {
                    return result;
                }

                _logger.LogInformation("Pago de préstamo ID {LoanId} procesado exitosamente por monto efectivo RD${EffectiveAmount}", dto.LoanId, effectiveAmount);
                
                var emailSent = await SendLoanPaymentEmailAsync(originAccount, loan, effectiveAmount, clientId);
                if (!emailSent)
                {
                    result.Value!.WarningMessage = "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico inesperado al procesar el pago de préstamo para el cliente {ClientId}", clientId);
                return ValidationResult<TransactionResultDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        #region Helper Methods

        private async Task<ValidationResult<TransactionResultDto>> ExecuteCreditCardPaymentAsync(SavingsAccount originAccount, CreditCard creditCard, decimal requestedAmount, decimal effectiveAmount, string clientId)
        {
            originAccount.Balance -= effectiveAmount;
            creditCard.OwedAmount -= effectiveAmount;

            await _savingsAccountRepository.UpdateAsync(originAccount);
            await _creditCardRepository.UpdateAsync(creditCard);

            var debitTx = new Transaction
            {
                SavingsAccountId = originAccount.Id,
                Amount = effectiveAmount,
                TransactionType = TransactionType.Debito,
                OperationType = OperationType.PagoTarjeta,
                Origin = originAccount.AccountNumber,
                Beneficiary = creditCard.CardNumber,
                Status = TransactionStatus.Aprobada,
                PerformedByUserId = clientId,
                Channel = ChannelPayment.Cliente,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            var cardPayment = new CardPayment
            {
                CreditCardId = creditCard.Id,
                TransactionId = 0,
                Transaction = debitTx,
                RequestedAmount = requestedAmount,
                EffectiveAmount = effectiveAmount,
                Channel = ChannelPayment.Cliente,
                PerformedByUserId = clientId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            await _transactionRepository.AddAsync(debitTx);
            await _cardPaymentRepository.AddAsync(cardPayment);

            var saveResult = await _transactionRepository.SaveChangesAsync();
            if (saveResult <= 0)
            {
                _logger.LogWarning("Error de persistencia: no fue posible guardar el pago de tarjeta de crédito ID {CardId}", creditCard.Id);
                return ValidationResult<TransactionResultDto>.Failure(GeneralError.UnexpectedError);
            }

            var resultDto = new TransactionResultDto
            {
                EffectiveAmount = effectiveAmount,
                TransactionType = TransactionType.Debito,
                Status = TransactionStatus.Aprobada,
                CreatedAt = debitTx.CreatedAt
            };

            return ValidationResult<TransactionResultDto>.Success(resultDto);
        }

        private async Task<ValidationResult<TransactionResultDto>> ExecuteLoanPaymentAsync(SavingsAccount originAccount, Loan loan, List<LoanInstallment> installments, decimal requestedAmount, decimal effectiveAmount, string clientId)
        {
            originAccount.Balance -= effectiveAmount;
            await _savingsAccountRepository.UpdateAsync(originAccount);

            var debitTx = new Transaction
            {
                SavingsAccountId = originAccount.Id,
                Amount = effectiveAmount,
                TransactionType = TransactionType.Debito,
                OperationType = OperationType.PagoPrestamo,
                Origin = originAccount.AccountNumber,
                Beneficiary = loan.LoanNumber,
                Status = TransactionStatus.Aprobada,
                PerformedByUserId = clientId,
                Channel = ChannelPayment.Cliente,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            await _transactionRepository.AddAsync(debitTx);

<<<<<<< HEAD
            ApplyInstallmentPayments(installments, effectiveAmount, clientId);
=======
            ApplyInstallmentPayments(installments, requestedAmount, effectiveAmount, clientId, debitTx);
>>>>>>> origin/development

            loan.PendingAmount -= effectiveAmount;
            if (loan.PendingAmount <= 0)
            {
                loan.Status = LoanStatus.Completado;
                _logger.LogInformation("El préstamo ID {LoanId} ha sido completamente liquidado. Cambiando estado a Completado.", loan.Id);
            }

            await _loansRepository.UpdateAsync(loan);

            var saveResult = await _loanInstallmentRepository.SaveChangesAsync();
            if (saveResult <= 0)
            {
                _logger.LogWarning("Error de persistencia: no fue posible guardar los cambios de pago del préstamo ID {LoanId}", loan.Id);
                return ValidationResult<TransactionResultDto>.Failure(GeneralError.UnexpectedError);
            }

            var resultDto = new TransactionResultDto
            {
                EffectiveAmount = effectiveAmount,
                TransactionType = TransactionType.Debito,
                Status = TransactionStatus.Aprobada,
                CreatedAt = debitTx.CreatedAt
            };

            return ValidationResult<TransactionResultDto>.Success(resultDto);
        }

<<<<<<< HEAD
        private void ApplyInstallmentPayments(List<LoanInstallment> installments, decimal effectiveAmount, string clientId)
=======
        private void ApplyInstallmentPayments(List<LoanInstallment> installments, decimal requestedAmount, decimal effectiveAmount, string clientId, Transaction debitTx)
>>>>>>> origin/development
        {
            var remainingPayment = effectiveAmount;
            var now = DateTimeOffset.UtcNow;

            foreach (var inst in installments)
            {
                if (remainingPayment <= 0) break;

                var paymentToApply = Math.Min(remainingPayment, inst.PendingBalance);
                inst.PendingBalance -= paymentToApply;
                remainingPayment -= paymentToApply;

                if (inst.PendingBalance <= 0)
                {
                    inst.paymentStatus = PaymentStatus.Pagada;
                    inst.IsOverdue = false;
                    inst.PaidAt = now;
                }

                _loanInstallmentRepository.UpdateAsync(inst);

                var loanPayment = new LoanPayment
                {
                    LoandId = inst.LoanId,
                    LoanInstallmentId = inst.Id,
<<<<<<< HEAD
                    EffectiveAmount = paymentToApply,
                    PaidAt = now,
                    Channel = ChannelPayment.Cliente,
                    PerformedByUserId = clientId
=======
                    RequestedAmount = requestedAmount,
                    EffectiveAmount = paymentToApply,
                    PaidAt = now,
                    Channel = ChannelPayment.Cliente,
                    PerformedByUserId = clientId,
                    Transaction = debitTx,
                    CreatedAt = now,
                    CreateByUserId = clientId  
>>>>>>> origin/development
                };

                _loansPaymentRepository.AddAsync(loanPayment);
            }
        }

        private async Task<bool> SendCreditCardPaymentEmailAsync(SavingsAccount origin, CreditCard card, decimal amount, string clientId)
        {
            var email = $"{clientId}@artemis.com";
            var lastFourCard = card.CardNumber.Length >= 4 ? card.CardNumber.Substring(card.CardNumber.Length - 4) : card.CardNumber;
            var lastFourOrig = origin.AccountNumber.Length >= 4 ? origin.AccountNumber.Substring(origin.AccountNumber.Length - 4) : origin.AccountNumber;

            try
            {
                _logger.LogInformation("Enviando correo de notificación de pago a tarjeta ****{LastFourCard}", lastFourCard);
                var sent = await _emailServices.SendNotification(new MessageDto
                {
                    To = email,
                    Subject = $"Pago realizado a la tarjeta {lastFourCard}",
                    Message = $"Monto pagado: RD${amount:N2}, Cuenta Origen: ****{lastFourOrig}, Tarjeta: ****{lastFourCard}, Fecha: {DateTimeOffset.UtcNow}"
                });
                return sent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo al enviar notificación por correo de pago de tarjeta.");
                return false;
            }
        }

        private async Task<bool> SendLoanPaymentEmailAsync(SavingsAccount origin, Loan loan, decimal amount, string clientId)
        {
            var email = $"{clientId}@artemis.com";
            var lastFourOrig = origin.AccountNumber.Length >= 4 ? origin.AccountNumber.Substring(origin.AccountNumber.Length - 4) : origin.AccountNumber;

            try
            {
                _logger.LogInformation("Enviando correo de notificación de pago a préstamo {LoanNumber}", loan.LoanNumber);
                var sent = await _emailServices.SendNotification(new MessageDto
                {
                    To = email,
                    Subject = $"Pago realizado al préstamo {loan.LoanNumber}",
                    Message = $"Monto pagado: RD${amount:N2}, Préstamo: {loan.LoanNumber}, Cuenta Origen: ****{lastFourOrig}, Fecha: {DateTimeOffset.UtcNow}"
                });
                return sent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo al enviar notificación por correo de pago de préstamo.");
                return false;
            }
        }

        #endregion
    }
}
