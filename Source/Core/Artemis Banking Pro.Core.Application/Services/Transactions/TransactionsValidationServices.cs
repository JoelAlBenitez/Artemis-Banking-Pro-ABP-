using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
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
            ILogger<TransactionsValidationServices> _logger)
        {
            _savingsAccountRepository = savingsAccountRepository;
            _creditCardRepository = creditCardRepository;
            _loansRepository = loansRepository;
            _beneficiaryRepository = beneficiaryRepository;
            _loanInstallmentRepository = loanInstallmentRepository;
            this._logger = _logger;
        }

        public async Task<ValidationResult<(SavingsAccount Origin, SavingsAccount Destination)>> ValidateExpressAsync(ExpressTransactionDto dto, string clientId)
        {
            if (dto.Amount <= 0)
            {
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.InvalidAmount);
            }

            var originAccount = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber && a.CustomerId == clientId && a.Status == SavingsAccountStatus.Activa);
            if (originAccount is null)
            {
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.OriginAccountNotFound);
            }

            var destAccount = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.DestinationAccountNumber && a.Status == SavingsAccountStatus.Activa);
            if (destAccount is null)
            {
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.DestinationAccountNotFound);
            }

            if (originAccount.Id == destAccount.Id)
            {
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.SameAccountTransfer);
            }

            if (originAccount.Balance < dto.Amount)
            {
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.InsufficientFunds);
            }

            return ValidationResult<(SavingsAccount, SavingsAccount)>.Success((originAccount, destAccount));
        }

        public async Task<ValidationResult<(SavingsAccount Origin, SavingsAccount Destination)>> ValidateBeneficiaryAsync(BeneficiaryTransactionDto dto, string clientId)
        {
            if (dto.Amount <= 0)
            {
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.InvalidAmount);
            }

            var beneficiary = await _beneficiaryRepository.GetFirstAsync(b => b.Id == dto.BeneficiaryId && b.OwnerClientId == clientId);
            if (beneficiary is null)
            {
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.BeneficiaryNotFound);
            }

            var originAccount = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber && a.CustomerId == clientId && a.Status == SavingsAccountStatus.Activa);
            if (originAccount is null)
            {
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.OriginAccountNotFound);
            }

            var destAccount = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == beneficiary.BeneficiaryAccountNumber && a.Status == SavingsAccountStatus.Activa);
            if (destAccount is null)
            {
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.DestinationAccountNotFound);
            }

            if (originAccount.Id == destAccount.Id)
            {
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.SameAccountTransfer);
            }

            if (originAccount.Balance < dto.Amount)
            {
                return ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.InsufficientFunds);
            }

            return ValidationResult<(SavingsAccount, SavingsAccount)>.Success((originAccount, destAccount));
        }

        public async Task<ValidationResult<(SavingsAccount Origin, CreditCard Card, decimal EffectiveAmount)>> ValidateCreditCardPaymentAsync(PayCreditCardDto dto, string clientId)
        {
            if (dto.Amount <= 0)
            {
                return ValidationResult<(SavingsAccount, CreditCard, decimal)>.Failure(TransactionError.InvalidAmount);
            }

            var originAccount = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber && a.CustomerId == clientId && a.Status == SavingsAccountStatus.Activa);
            if (originAccount is null)
            {
                return ValidationResult<(SavingsAccount, CreditCard, decimal)>.Failure(TransactionError.OriginAccountNotFound);
            }

            var creditCard = await _creditCardRepository.GetFirstAsync(c => c.Id == dto.CreditCardId && c.CustomerId == clientId && c.Status == CreditCardStatus.Activa);
            if (creditCard is null)
            {
                return ValidationResult<(SavingsAccount, CreditCard, decimal)>.Failure(TransactionError.CreditCardNotFound);
            }

            if (creditCard.OwedAmount <= 0)
            {
                return ValidationResult<(SavingsAccount, CreditCard, decimal)>.Failure(TransactionError.CreditCardOverpayment);
            }

            var effectiveAmount = Math.Min(dto.Amount, creditCard.OwedAmount);

            if (originAccount.Balance < effectiveAmount)
            {
                return ValidationResult<(SavingsAccount, CreditCard, decimal)>.Failure(TransactionError.InsufficientFunds);
            }

            return ValidationResult<(SavingsAccount, CreditCard, decimal)>.Success((originAccount, creditCard, effectiveAmount));
        }

        public async Task<ValidationResult<(SavingsAccount Origin, Loan Loan, List<LoanInstallment> Installments, decimal EffectiveAmount)>> ValidateLoanPaymentAsync(PayLoanDto dto, string clientId)
        {
            if (dto.Amount <= 0)
            {
                return ValidationResult<(SavingsAccount, Loan, List<LoanInstallment>, decimal)>.Failure(TransactionError.InvalidAmount);
            }

            var originAccount = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber && a.CustomerId == clientId && a.Status == SavingsAccountStatus.Activa);
            if (originAccount is null)
            {
                return ValidationResult<(SavingsAccount, Loan, List<LoanInstallment>, decimal)>.Failure(TransactionError.OriginAccountNotFound);
            }

            var loan = await _loansRepository.GetFirstAsync(l => l.Id == dto.LoanId && l.CustomerId == clientId && l.Status == LoanStatus.Activo);
            if (loan is null)
            {
                return ValidationResult<(SavingsAccount, Loan, List<LoanInstallment>, decimal)>.Failure(TransactionError.LoanNotFound);
            }

            var installments = (await _loanInstallmentRepository.GetAllFindAsync(i => i.LoanId == loan.Id && i.paymentStatus != PaymentStatus.Pagada))
                .OrderBy(i => i.DueDate)
                .ToList();

            if (installments.Count == 0)
            {
                return ValidationResult<(SavingsAccount, Loan, List<LoanInstallment>, decimal)>.Failure(TransactionError.LoanOverpayment);
            }

            var totalPending = installments.Sum(i => i.PendingBalance);
            var effectiveAmount = Math.Min(dto.Amount, totalPending);

            if (originAccount.Balance < effectiveAmount)
            {
                return ValidationResult<(SavingsAccount, Loan, List<LoanInstallment>, decimal)>.Failure(TransactionError.InsufficientFunds);
            }

            return ValidationResult<(SavingsAccount, Loan, List<LoanInstallment>, decimal)>.Success((originAccount, loan, installments, effectiveAmount));
        }
    }
}
