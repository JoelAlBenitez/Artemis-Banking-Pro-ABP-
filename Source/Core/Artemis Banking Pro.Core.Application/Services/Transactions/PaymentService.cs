using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
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
        private readonly ILoansPaymentRepository _loanPaymentRepository;
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
            ILoansPaymentRepository loanPaymentRepository,
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
            _loanPaymentRepository = loanPaymentRepository;
            _validationServices = validationServices;
            _emailServices = emailServices;
            _logger = logger;
        }

        public async Task<ValidationResult<TransactionResultDto>> PayCreditCardAsync(PayCreditCardDto dto, string clientId)
        {
            var validation = await _validationServices.ValidateCreditCardPaymentAsync(dto, clientId);
            if (!validation.IsValid)
            {
                if (validation.Errors.Contains(TransactionError.InsufficientFunds))
                {
                    var srcAcc = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber);
                    var cc = await _creditCardRepository.GetFirstAsync(c => c.Id == dto.CreditCardId);

                    if (srcAcc is not null && cc is not null)
                    {
                        var effAmount = Math.Min(dto.Amount, cc.OwedAmount);
                        var rejectedTx = new Transaction
                        {
                            SavingsAccountId = srcAcc.Id,
                            Amount = effAmount,
                            TransactionType = TransactionType.Debito,
                            OperationType = OperationType.PagoTarjeta,
                            Origin = srcAcc.AccountNumber,
                            Beneficiary = cc.LastFourDigits,
                            Status = TransactionStatus.Rechazada,
                            RejectionReason = "Fondos insuficientes",
                            PerformedByUserId = clientId,
                            Channel = ChannelPayment.Cliente,
                            CreatedAt = DateTimeOffset.UtcNow,
                            CreateByUserId = clientId
                        };

                        await _transactionRepository.AddAsync(rejectedTx);
                        await _transactionRepository.SaveChangesAsync();
                    }
                }

                return ValidationResult<TransactionResultDto>.Failure(validation.Errors.ToList());
            }

            var (originAccount, creditCard, effectiveAmount) = validation.Value;

            originAccount.Balance -= effectiveAmount;
            creditCard.OwedAmount -= effectiveAmount;

            await _savingsAccountRepository.UpdateAsync(originAccount);
            await _creditCardRepository.UpdateAsync(creditCard);

            var transaction = new Transaction
            {
                SavingsAccountId = originAccount.Id,
                Amount = effectiveAmount,
                TransactionType = TransactionType.Debito,
                OperationType = OperationType.PagoTarjeta,
                Origin = originAccount.AccountNumber,
                Beneficiary = creditCard.LastFourDigits,
                Status = TransactionStatus.Aprobada,
                PerformedByUserId = clientId,
                Channel = ChannelPayment.Cliente,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            await _transactionRepository.AddAsync(transaction);
            await _transactionRepository.SaveChangesAsync();

            var cardPayment = new CardPayment
            {
                CreditCardId = creditCard.Id,
                TransactionId = transaction.Id,
                RequestedAmount = dto.Amount,
                EffectiveAmount = effectiveAmount,
                Channel = ChannelPayment.Cliente,
                PerformedByUserId = clientId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            await _cardPaymentRepository.AddAsync(cardPayment);
            await _cardPaymentRepository.SaveChangesAsync();

            // Pendiente: Obtener correos reales desde IIdentityServices cuando Adrian complete el módulo
            var emisorEmail = $"{clientId}@artemis.com";
            var lastFourOrig = originAccount.AccountNumber.Length >= 4 ? originAccount.AccountNumber.Substring(originAccount.AccountNumber.Length - 4) : originAccount.AccountNumber;

            try
            {
                await _emailServices.SendNotification(new MessageDto
                {
                    To = emisorEmail,
                    Subject = $"Pago realizado a la tarjeta {creditCard.LastFourDigits}",
                    Message = $"Monto Pagado: RD${effectiveAmount:N2}, Cuenta Origen: ****{lastFourOrig}, Tarjeta Pagada: ****{creditCard.LastFourDigits}, Fecha: {transaction.CreatedAt}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.");
            }

            var resultDto = new TransactionResultDto
            {
                EffectiveAmount = effectiveAmount,
                TransactionType = "DÉBITO",
                Status = "APROBADA",
                CreatedAt = transaction.CreatedAt
            };

            return ValidationResult<TransactionResultDto>.Success(resultDto);
        }

        public async Task<ValidationResult<TransactionResultDto>> PayLoanAsync(PayLoanDto dto, string clientId)
        {
            var validation = await _validationServices.ValidateLoanPaymentAsync(dto, clientId);
            if (!validation.IsValid)
            {
                if (validation.Errors.Contains(TransactionError.InsufficientFunds))
                {
                    var srcAcc = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber);
                    var ln = await _loansRepository.GetFirstAsync(l => l.Id == dto.LoanId);

                    if (srcAcc is not null && ln is not null)
                    {
                        var insts = (await _loanInstallmentRepository.GetAllFindAsync(i => i.LoanId == ln.Id && i.paymentStatus != PaymentStatus.Pagada))
                            .OrderBy(i => i.DueDate)
                            .ToList();

                        var totalPending = insts.Sum(i => i.PendingBalance);
                        var effAmount = Math.Min(dto.Amount, totalPending);

                        var rejectedTx = new Transaction
                        {
                            SavingsAccountId = srcAcc.Id,
                            Amount = effAmount,
                            TransactionType = TransactionType.Debito,
                            OperationType = OperationType.PagoPrestamo,
                            Origin = srcAcc.AccountNumber,
                            Beneficiary = ln.LoanNumber,
                            Status = TransactionStatus.Rechazada,
                            RejectionReason = "Fondos insuficientes",
                            PerformedByUserId = clientId,
                            Channel = ChannelPayment.Cliente,
                            CreatedAt = DateTimeOffset.UtcNow,
                            CreateByUserId = clientId
                        };

                        await _transactionRepository.AddAsync(rejectedTx);
                        await _transactionRepository.SaveChangesAsync();
                    }
                }

                return ValidationResult<TransactionResultDto>.Failure(validation.Errors.ToList());
            }

            var (originAccount, loan, installments, effectiveAmount) = validation.Value;

            originAccount.Balance -= effectiveAmount;
            await _savingsAccountRepository.UpdateAsync(originAccount);

            var transaction = new Transaction
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

            await _transactionRepository.AddAsync(transaction);
            await _transactionRepository.SaveChangesAsync();

            var remainingAmount = effectiveAmount;
            foreach (var installment in installments)
            {
                if (remainingAmount <= 0) break;

                var paymentForThisInstallment = Math.Min(remainingAmount, installment.PendingBalance);
                installment.PendingBalance -= paymentForThisInstallment;

                if (installment.PendingBalance <= 0)
                {
                    installment.paymentStatus = PaymentStatus.Pagada;
                    installment.IsOverdue = false;
                    installment.PaidAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    installment.paymentStatus = PaymentStatus.ParcialmentePagada;
                }

                await _loanInstallmentRepository.UpdateAsync(installment);

                var loanPayment = new LoanPayment
                {
                    LoandId = loan.Id,
                    LoanInstallmentId = installment.Id,
                    EffectiveAmount = paymentForThisInstallment,
                    Channel = ChannelPayment.Cliente,
                    PerformedByUserId = clientId,
                    PaidAt = DateTimeOffset.UtcNow
                };

                await _loanPaymentRepository.AddAsync(loanPayment);
                remainingAmount -= paymentForThisInstallment;
            }

            loan.PendingAmount -= effectiveAmount;

            var hasUnpaid = await _loanInstallmentRepository.ExistElementByConsult(i => i.LoanId == loan.Id && i.paymentStatus != PaymentStatus.Pagada);
            if (!hasUnpaid)
            {
                loan.Status = LoanStatus.Completado;
                loan.CompletedAt = DateTimeOffset.UtcNow;
            }

            await _loansRepository.UpdateAsync(loan);
            await _loanInstallmentRepository.SaveChangesAsync();

            // Pendiente: Obtener correos reales desde IIdentityServices cuando Adrian complete el módulo
            var emisorEmail = $"{clientId}@artemis.com";
            var lastFourOrig = originAccount.AccountNumber.Length >= 4 ? originAccount.AccountNumber.Substring(originAccount.AccountNumber.Length - 4) : originAccount.AccountNumber;

            try
            {
                await _emailServices.SendNotification(new MessageDto
                {
                    To = emisorEmail,
                    Subject = $"Pago realizado al préstamo {loan.LoanNumber}",
                    Message = $"Monto Pagado: RD${effectiveAmount:N2}, Cuenta Origen: ****{lastFourOrig}, Préstamo: {loan.LoanNumber}, Fecha: {transaction.CreatedAt}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.");
            }

            var resultDto = new TransactionResultDto
            {
                EffectiveAmount = effectiveAmount,
                TransactionType = "DÉBITO",
                Status = "APROBADA",
                CreatedAt = transaction.CreatedAt
            };

            return ValidationResult<TransactionResultDto>.Success(resultDto);
        }
    }
}
