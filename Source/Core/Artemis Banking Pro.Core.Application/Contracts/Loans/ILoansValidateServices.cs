using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Loans;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Loans
{
    public interface ILoansValidateServices
    {
        //Administrador autenticado responsable de la operación. Devuelve su Id de Identity.
        ValidationResult<string> ValidateAdministratorInSession();

        //Devuelve el préstamo con sus cuotas ya cargadas: el servicio no lo vuelve a consultar
        Task<ValidationResult<Loan>> EditValidateAnnualInterestRateAsync(int Id);
        Task<ValidationResult> AssigmentLoansValidateAsync(LoansAssignmentDto assignment);

        //Búsqueda por cédula del listado. Devuelve el Id del cliente en Identity cuando se
        //buscó por cédula, y null cuando el listado va sin filtro de cliente.
        Task<ValidationResult<string?>> GetLoansByCustomerValidateAsync(LoansFilterDto dto);
    }
}