using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Loans;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Loans
{
    public interface ILoansValidateServices
    {
<<<<<<< HEAD
        Task<ValidationResult> EditValidateAnnualInterestRateAsync(int Id);
=======
        //Devuelve el préstamo con sus cuotas ya cargadas: el servicio no lo vuelve a consultar
        Task<ValidationResult<Loan>> EditValidateAnnualInterestRateAsync(int Id);
>>>>>>> origin/development
        Task<ValidationResult> AssigmentLoansValidateAsync(LoansAssignmentDto assignment);
        Task<ValidationResult> GetLoansByCustomerValidateAsync(LoansFilterDto dto);
    }
}
