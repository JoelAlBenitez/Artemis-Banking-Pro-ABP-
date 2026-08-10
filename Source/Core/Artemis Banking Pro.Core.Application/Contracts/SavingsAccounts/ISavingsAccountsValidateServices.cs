using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;

namespace Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts
{
    //Único lugar donde viven las reglas de negocio del módulo de cuentas de ahorro.
    public interface ISavingsAccountsValidateServices
    {
        //Administrador autenticado responsable de la operación. Devuelve su Id de Identity.
        ValidationResult<string> ValidateAdministratorInSession();

        //Paso 1 de la asignación: cliente seleccionado, activo y con principal activa
        Task<ValidationResult> ValidateCustomerSelectionAsync(string customerId);

        //Paso 2 de la asignación: cliente válido y balance inicial mayor o igual a cero
        Task<ValidationResult> ValidateAssignmentAsync(SavingsAccountAssignmentDto dto);

        //Existencia y estado de la cuenta consultada
        Task<ValidationResult<SavingsAccount>> ValidateActiveSavingsAccountAsync(int savingsAccountId);

        //Cancelación: cuenta existente, activa, secundaria y con principal activa receptora.
        //Devuelve la cuenta secundaria rastreada para aplicar la transferencia y el cambio de estado.
        Task<ValidationResult<SavingsAccount>> ValidateCancellationAsync(int savingsAccountId);

        //Búsqueda por cédula del listado principal. Devuelve el Id del cliente en Identity
        //cuando se buscó por cédula, y null cuando el listado va sin filtro de cliente.
        Task<ValidationResult<string?>> ValidateCustomerAccountsQueryAsync(SavingsAccountFilterDto filter);
    }
}
