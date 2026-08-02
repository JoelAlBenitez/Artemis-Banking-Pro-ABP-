using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;

namespace ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts
{
    //Solo se declaran los miembros que el repositorio genérico no puede resolver con lambdas.
    //La existencia de un número, la cuenta principal activa y demás consultas se obtienen con
    //ExistElementByConsult y GetFirstAsync.
    public interface ISavingsAccountsRepository :
        IGenericRepository<SavingsAccount, int>
    {
        //Orden propio del módulo: sin filtro de estado, activas primero y luego canceladas
        Task<PagedResult<SavingsAccount>> GetPagedSavingsAccountsAsync(
            int page,
            int pageSize,
            SavingsAccountStatus? status,
            SavingsAccountType? accountType,
            string? customerId);

        //Emite el siguiente número de 9 dígitos desde SavingsAccountNumberSequence. Un solo
        //viaje a la base de datos: no recorre registros ni reintenta.
        Task<string> GetNextAccountNumberAsync();
    }
}
