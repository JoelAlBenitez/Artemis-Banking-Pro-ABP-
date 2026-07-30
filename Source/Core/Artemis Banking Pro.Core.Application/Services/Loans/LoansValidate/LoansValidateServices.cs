using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using ArtemisBankingPro.Core.Domain.CodeErrors.LoansErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.Loans.LoansValidate
{
    public class LoansValidateServices : ILoansValidateServices
    {
        private readonly ILoansRepository _loansRepository;
        private readonly ILogger<LoansValidateServices> _logger;
        //integrar el ICurrentUserServices 
        //

        public LoansValidateServices(ILogger<LoansValidateServices> logger, 
            ILoansRepository loansRepository)
        {
            _loansRepository = loansRepository;
            _logger = logger;

        }

        public async Task<ValidationResult> AssigmentLoansValidateAsync(LoansAssignmentDto assignment)
        {

          
           if(assignment == null)
           {
                //modificar el 0 por el id del current user admin cuando sea desarrollado el ICurrentUserServices
                _logger.LogWarning("Datos de asignacion invalidos, realizados por el administrador con ID {ID} ", 0);
                return ValidationResult.Failure(LoandError.DataInvalid);
           }
            //agregar aqui existencia del usuario del assigment

            _logger.LogInformation("Verificaciones de prestamos activos del cliente con ID {ID}", assignment.CustomerId);
            var exist =  await _loansRepository.ExistElementByConsult(x => x.CustomerId == assignment.CustomerId 
            && x.Status == LoanStatus.Activo);
            if (exist)
            {
                _logger.LogWarning("Cliente con IDm{ID} ya posee un prestamo activo", assignment.CustomerId);
                return ValidationResult.Failure(LoandError.CustomerWithLoanExist);
            }
            return ValidationResult.Success();
        }

        public async Task<ValidationResult> EditValidateAnnualInterestRateAsync(int Id)
        {
            var loan = await _loansRepository.GetByIdAsync(Id);
            if (loan is null)
            {
                _logger.LogWarning("Prestamo con el ID {ID} no encontrado", Id);
                return ValidationResult<EditAnnualInterestRateDto>.Failure(LoandError.NonExistsLoan);
            }

            if (loan.Status != LoanStatus.Activo)
            {
                _logger.LogWarning("Prestamo con el ID {ID} no posee un estado de consulta valido {Estado}",
                    loan.Id, loan.Status.ToString()
                  );
                return ValidationResult<EditAnnualInterestRateDto>.Failure(LoandError.LoanIsNotActive);
            }
            _logger.LogInformation("Prestamo con ID {ID} encontrado y valido para ser operado", loan.Id);
            return ValidationResult.Success();
        }

        public Task<ValidationResult> GetLoansByCustomerValidateAsync(ConsultClientByIdCardDto customer)
        {
            throw new NotImplementedException();
        }

        public Task<ValidationResult> GetLoansByStatusInCustomerValidateAsync(string customerId, LoanStatusFilter loanStatusFilter)
        {
            throw new NotImplementedException();
        }

        //agregar private metodo que determine si el current user tiene Role -> Administrador
    }
}
