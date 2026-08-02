using Artemis_Banking_Pro.Core.Application.Contracts.Generic;
using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;

namespace Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts
{
    //herencia se debe crear en la implementacion
    public interface ISavingsAccountsServices :
        IGenericServices<SavingsAccountAssignmentDto, SavingsAccountDto, int>
    {
        Task<ValidationResult<PagedResult<SavingsAccountDto>>> GetPagedSavingsAccountsAsync(
            SavingsAccountFilterDto filter, string? customerId);

        Task<ValidationResult<PagedResult<TransactionDto>>> GetPagedTransactionsAsync(
            int savingsAccountId, int page);

        Task<ValidationResult> AssignSavingsAccountAsync(SavingsAccountAssignmentDto dto);

        Task<ValidationResult> CancelSavingsAccountAsync(int savingsAccountId);
    }
}
