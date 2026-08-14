using Artemis_Banking_Pro.Core.Application.Contracts.Generic;
using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;

namespace Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts
{

    public interface ISavingsAccountsServices :
        IGenericServices<SavingsAccountAssignmentDto, SavingsAccountDto, int>
    {
        //La cédula del filtro se traduce internamente al Id del cliente en Identity
        Task<ValidationResult<PagedResult<SavingsAccountDto>>> GetPagedSavingsAccountsAsync(
            SavingsAccountFilterDto filter);

        //Paso 1 de la asignación: clientes activos con su deuda total, filtrables por cédula
        Task<ValidationResult<IReadOnlyCollection<ClientSavingsAccountDto>>> GetActiveClientsAsync(
            string? idCard = null);

        Task<ValidationResult<PagedResult<TransactionDto>>> GetPagedTransactionsAsync(
            int savingsAccountId, int page);

        Task<ValidationResult> AssignSavingsAccountAsync(SavingsAccountAssignmentDto dto);

        Task<ValidationResult> CancelSavingsAccountAsync(int savingsAccountId);

    
        Task<bool> IsAccountActiveAsync(string accountNumber);

        Task<decimal> GetCustomerTotalDebtAmountAsync(string customerId);
    }
}
