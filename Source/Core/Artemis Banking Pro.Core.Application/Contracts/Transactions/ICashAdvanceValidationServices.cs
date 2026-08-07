using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Transactions
{
    public interface ICashAdvanceValidationServices
    {
        Task<ValidationResult<(CreditCard Card, SavingsAccount Account, decimal InterestAmount, decimal TotalCharged)>> ValidateCashAdvanceAsync(
            CashAdvanceRequestDto dto, 
            string clientId);
    }
}
