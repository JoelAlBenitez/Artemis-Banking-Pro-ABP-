using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.Transactions
{
    public sealed class TransactionsValidationServices : ITransactionsValidationServices
    {
        private readonly ISavingsAccountsRepository _savingsAccountRepository;
        private readonly ICreditCardsRepository _creditCardRepository;
        private readonly ILoansRepository _loansRepository;
        private readonly IBeneficiaryRepository _beneficiaryRepository;
        private readonly ILoanInstallmentRepository _loanInstallmentRepository;
        private readonly ILogger<TransactionsValidationServices> _logger;

        public TransactionsValidationServices(
            ISavingsAccountsRepository savingsAccountRepository,
            ICreditCardsRepository creditCardRepository,
            ILoansRepository loansRepository,
            IBeneficiaryRepository beneficiaryRepository,
            ILoanInstallmentRepository loanInstallmentRepository,
            ILogger<TransactionsValidationServices> logger)
        {
            _savingsAccountRepository = savingsAccountRepository;
            _creditCardRepository = creditCardRepository;
            _loansRepository = loansRepository;
            _beneficiaryRepository = beneficiaryRepository;
            _loanInstallmentRepository = loanInstallmentRepository;
            _logger = logger;
        }

        public async Task<ValidationResult<(SavingsAccount Origin, SavingsAccount Destination)>> ValidateExpressAsync(ExpressTransactionDto dto, string clientId)
        {
            _logger.LogInformation("Iniciando validación de transferencia express para el cliente {ClientId} por monto RD${Amount}", clientId, dto.Amount);

            if (dto.Amount <= 0)
            {
                _logger.LogWarning("Validación fallida: el monto ingresado {Amount} debe ser mayor que cero", dto.Amount);
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.InvalidAmount);
            }

            try
            {
                var originAccount = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber && a.CustomerId == clientId && a.Status == SavingsAccountStatus.Activa);
                if (originAccount is null)
                {
                    _logger.LogWarning("Validación fallida: la cuenta de origen {SourceAccountNumber} no fue encontrada o no pertenece al cliente {ClientId}", dto.SourceAccountNumber, clientId);
                    return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.OriginAccountNotFound);
                }

                var destAccount = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.DestinationAccountNumber && a.Status == SavingsAccountStatus.Activa);
                if (destAccount is null)
                {
                    _logger.LogWarning("Validación fallida: la cuenta de destino {DestinationAccountNumber} no existe o no está activa", dto.DestinationAccountNumber);
                    return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.DestinationAccountNotFound);
                }

                if (originAccount.Id == destAccount.Id)
                {
                    _logger.LogWarning("Validación fallida: intento de transferencia entre la misma cuenta {AccountNumber}", dto.SourceAccountNumber);
                    return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.SameAccountTransfer);
                }

                if (originAccount.Balance < dto.Amount)
                {
                    _logger.LogWarning("Validación fallida: fondos insuficientes en la cuenta {AccountNumber}. Balance actual RD${Balance}, monto requerido RD${Amount}", originAccount.AccountNumber, originAccount.Balance, dto.Amount);
                    return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.InsufficientFunds);
                }

                _logger.LogInformation("Validación de transferencia express exitosa para el cliente {ClientId}", clientId);
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Success((originAccount, destAccount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al validar la transferencia express del cliente {ClientId}", clientId);
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<(SavingsAccount Origin, SavingsAccount Destination)>> ValidateBeneficiaryAsync(BeneficiaryTransactionDto dto, string clientId)
        {
            _logger.LogInformation("Iniciando validación de transferencia a beneficiario {BeneficiaryId} para el cliente {ClientId} por monto RD${Amount}", dto.BeneficiaryId, clientId, dto.Amount);

            if (dto.Amount <= 0)
            {
                _logger.LogWarning("Validación fallida: el monto ingresado {Amount} debe ser mayor que cero", dto.Amount);
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.InvalidAmount);
            }

            try
            {
                var beneficiary = await _beneficiaryRepository.GetFirstAsync(b => b.Id == dto.BeneficiaryId && b.OwnerClientId == clientId);
                if (beneficiary is null)
                {
                    _logger.LogWarning("Validación fallida: el beneficiario {BeneficiaryId} no existe o no pertenece al cliente {ClientId}", dto.BeneficiaryId, clientId);
                    return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.BeneficiaryNotFound);
                }

                var originAccount = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber && a.CustomerId == clientId && a.Status == SavingsAccountStatus.Activa);
                if (originAccount is null)
                {
                    _logger.LogWarning("Validación fallida: la cuenta de origen {SourceAccountNumber} no fue encontrada o no pertenece al cliente {ClientId}", dto.SourceAccountNumber, clientId);
                    return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.OriginAccountNotFound);
                }

                var destAccount = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == beneficiary.BeneficiaryAccountNumber && a.Status == SavingsAccountStatus.Activa);
                if (destAccount is null)
                {
                    _logger.LogWarning("Validación fallida: la cuenta del beneficiario {BeneficiaryAccountNumber} no existe o no está activa", beneficiary.BeneficiaryAccountNumber);
                    return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.DestinationAccountNotFound);
                }

                if (originAccount.Id == destAccount.Id)
                {
                    _logger.LogWarning("Validación fallida: intento de transferencia entre la misma cuenta {AccountNumber}", dto.SourceAccountNumber);
                    return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.SameAccountTransfer);
                }

                if (originAccount.Balance < dto.Amount)
                {
                    _logger.LogWarning("Validación fallida: fondos insuficientes en la cuenta {AccountNumber}. Balance actual RD${Balance}, monto requerido RD${Amount}", originAccount.AccountNumber, originAccount.Balance, dto.Amount);
                    return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.InsufficientFunds);
                }

                _logger.LogInformation("Validación de transferencia a beneficiario exitosa para el cliente {ClientId}", clientId);
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Success((originAccount, destAccount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al validar la transferencia a beneficiario del cliente {ClientId}", clientId);
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<(SavingsAccount Origin, CreditCard Card, decimal EffectiveAmount)>> ValidateCreditCardPaymentAsync(PayCreditCardDto dto, string clientId)
        {
            _logger.LogInformation("Iniciando validación de pago de tarjeta de crédito {CreditCardId} para el cliente {ClientId} por monto RD${Amount}", dto.CreditCardId, clientId, dto.Amount);

            if (dto.Amount <= 0)
            {
                _logger.LogWarning("Validación fallida: el monto ingresado {Amount} debe ser mayor que cero", dto.Amount);
                return ValidationResult<(SavingsAccount, CreditCard, decimal)>.Failure(TransactionError.InvalidAmount);
            }

            try
            {
                var originAccount = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber && a.CustomerId == clientId && a.Status == SavingsAccountStatus.Activa);
                if (originAccount is null)
                {
                    _logger.LogWarning("Validación fallida: la cuenta de origen {SourceAccountNumber} no fue encontrada o no pertenece al cliente {ClientId}", dto.SourceAccountNumber, clientId);
                    return ValidationResult<(SavingsAccount, CreditCard, decimal)>.Failure(TransactionError.OriginAccountNotFound);
                }

                var creditCard = await _creditCardRepository.GetFirstAsync(c => c.Id == dto.CreditCardId && c.CustomerId == clientId && c.Status == CreditCardStatus.Activa);
                if (creditCard is null)
                {
                    _logger.LogWarning("Validación fallida: la tarjeta de crédito {CreditCardId} no existe o no pertenece al cliente {ClientId}", dto.CreditCardId, clientId);
                    return ValidationResult<(SavingsAccount, CreditCard, decimal)>.Failure(TransactionError.CreditCardNotFound);
                }

                if (creditCard.OwedAmount <= 0)
                {
                    _logger.LogWarning("Validación fallida: la tarjeta de crédito {CreditCardId} no posee deuda pendiente", dto.CreditCardId);
                    return ValidationResult<(SavingsAccount, CreditCard, decimal)>.Failure(TransactionError.CreditCardOverpayment);
                }

                var effectiveAmount = Math.Min(dto.Amount, creditCard.OwedAmount);

                if (originAccount.Balance < effectiveAmount)
                {
                    _logger.LogWarning("Validación fallida: fondos insuficientes en la cuenta {AccountNumber}. Balance actual RD${Balance}, monto requerido RD${Amount}", originAccount.AccountNumber, originAccount.Balance, effectiveAmount);
                    return ValidationResult<(SavingsAccount, CreditCard, decimal)>.Failure(TransactionError.InsufficientFunds);
                }

                _logger.LogInformation("Validación de pago de tarjeta exitosa para el cliente {ClientId}. Monto efectivo a pagar RD${EffectiveAmount}", clientId, effectiveAmount);
                return ValidationResult<(SavingsAccount, CreditCard, decimal)>.Success((originAccount, creditCard, effectiveAmount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al validar el pago de tarjeta del cliente {ClientId}", clientId);
                return ValidationResult<(SavingsAccount, CreditCard, decimal)>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<(SavingsAccount Origin, Loan Loan, List<LoanInstallment> Installments, decimal EffectiveAmount)>> ValidateLoanPaymentAsync(PayLoanDto dto, string clientId)
        {
            _logger.LogInformation("Iniciando validación de pago de préstamo {LoanId} para el cliente {ClientId} por monto RD${Amount}", dto.LoanId, clientId, dto.Amount);

            if (dto.Amount <= 0)
            {
                _logger.LogWarning("Validación fallida: el monto ingresado {Amount} debe ser mayor que cero", dto.Amount);
                return ValidationResult<(SavingsAccount, Loan, List<LoanInstallment>, decimal)>.Failure(TransactionError.InvalidAmount);
            }

            try
            {
                var originAccount = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber && a.CustomerId == clientId && a.Status == SavingsAccountStatus.Activa);
                if (originAccount is null)
                {
                    _logger.LogWarning("Validación fallida: la cuenta de origen {SourceAccountNumber} no fue encontrada o no pertenece al cliente {ClientId}", dto.SourceAccountNumber, clientId);
                    return ValidationResult<(SavingsAccount, Loan, List<LoanInstallment>, decimal)>.Failure(TransactionError.OriginAccountNotFound);
                }

                var loan = await _loansRepository.GetFirstAsync(l => l.Id == dto.LoanId && l.CustomerId == clientId && l.Status == LoanStatus.Activo);
                if (loan is null)
                {
                    _logger.LogWarning("Validación fallida: el préstamo {LoanId} no existe o no pertenece al cliente {ClientId}", dto.LoanId, clientId);
                    return ValidationResult<(SavingsAccount, Loan, List<LoanInstallment>, decimal)>.Failure(TransactionError.LoanNotFound);
                }

                var installments = (await _loanInstallmentRepository.GetAllFindAsync(i => i.LoanId == loan.Id && i.paymentStatus != PaymentStatus.Pagada))
                    .OrderBy(i => i.DueDate)
                    .ToList();

                if (installments.Count == 0)
                {
                    _logger.LogWarning("Validación fallida: el préstamo {LoanId} no posee cuotas pendientes", dto.LoanId);
                    return ValidationResult<(SavingsAccount, Loan, List<LoanInstallment>, decimal)>.Failure(TransactionError.LoanOverpayment);
                }

                var totalPending = installments.Sum(i => i.PendingBalance);
                var effectiveAmount = Math.Min(dto.Amount, totalPending);

                if (originAccount.Balance < effectiveAmount)
                {
                    _logger.LogWarning("Validación fallida: fondos insuficientes en la cuenta {AccountNumber}. Balance actual RD${Balance}, monto requerido RD${Amount}", originAccount.AccountNumber, originAccount.Balance, effectiveAmount);
                    return ValidationResult<(SavingsAccount, Loan, List<LoanInstallment>, decimal)>.Failure(TransactionError.InsufficientFunds);
                }

                _logger.LogInformation("Validación de pago de préstamo exitosa para el cliente {ClientId}. Monto efectivo a pagar RD${EffectiveAmount}", clientId, effectiveAmount);
                return ValidationResult<(SavingsAccount, Loan, List<LoanInstallment>, decimal)>.Success((originAccount, loan, installments, effectiveAmount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al validar el pago de préstamo del cliente {ClientId}", clientId);
                return ValidationResult<(SavingsAccount, Loan, List<LoanInstallment>, decimal)>.Failure(GeneralError.UnexpectedError);
            }
        }
    }
}
