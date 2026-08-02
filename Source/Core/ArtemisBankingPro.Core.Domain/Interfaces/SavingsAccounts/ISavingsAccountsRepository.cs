using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;

namespace ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts
{
    public interface ISavingsAccountsRepository :
        IGenericRepository<SavingsAccount, int>
    {
        Task<PagedResult<SavingsAccount>> GetPagedSavingsAccountsAsync(
            int page,
            int pageSize,
            SavingsAccountStatus? status,
            SavingsAccountType? accountType,
            string? customerId);

        //Receptora del balance remanente al cancelar una secundaria. Se recupera rastreada
        //porque su balance se modifica dentro de la misma unidad de guardado.
        Task<SavingsAccount?> GetActivePrimaryAccountAsync(string customerId, bool asNoTracking = true);

        //Unicidad del número de 9 dígitos dentro de las cuentas de ahorro
        Task<bool> ExistsAccountNumberAsync(string accountNumber);
    }
}
