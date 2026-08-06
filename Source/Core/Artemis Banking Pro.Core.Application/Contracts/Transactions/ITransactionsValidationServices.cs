using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Transactions
{
    public interface ITransactionsValidationServices
    {
        Task<ValidationResult<(SavingsAccount Origin, SavingsAccount Destination)>> ValidateExpressAsync(ExpressTransactionDto dto, string clientId);
        Task<ValidationResult<(SavingsAccount Origin, SavingsAccount Destination)>> ValidateBeneficiaryAsync(BeneficiaryTransactionDto dto, string clientId);
        Task<ValidationResult<(SavingsAccount Origin, CreditCard Card, decimal EffectiveAmount)>> ValidateCreditCardPaymentAsync(PayCreditCardDto dto, string clientId);
        Task<ValidationResult<(SavingsAccount Origin, Loan Loan, List<LoanInstallment> Installments, decimal EffectiveAmount)>> ValidateLoanPaymentAsync(PayLoanDto dto, string clientId);
    }
}
