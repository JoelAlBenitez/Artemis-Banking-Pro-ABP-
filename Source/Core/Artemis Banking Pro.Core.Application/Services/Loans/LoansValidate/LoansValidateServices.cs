using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
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

           if(assignment is null)
           {
                _logger.LogWarning("Datos de asignacion invalidos");
                return ValidationResult.Failure(GeneralError.DataInvalid);
           }
            //agregar aqui existencia del usuario del assigment
            _logger.LogInformation("Verificaciones de prestamos activos del cliente con ID {ID}", assignment.CustomerId);
            var exist =  await _loansRepository.ExistElementByConsult(x => x.CustomerId == assignment.CustomerId 
            && x.Status == LoanStatus.Activo);
            if (exist)
            {
                _logger.LogWarning("Cliente con ID {ID} ya posee un prestamo activo", assignment.CustomerId);
                return ValidationResult.Failure(LoansError.CustomerWithLoanExist);
            }
            if((int)assignment.TermLoans > 60 || (int)assignment.TermLoans < 6)
            {
                ValidationResult.Failure(LoansError.InvalidTerm);
            } // agregar _logger.LogWarrning
            bool termIsDefined = Enum.IsDefined(typeof(TermMonths), assignment.TermLoans);
            if (!termIsDefined)
            {
                ValidationResult.Failure(LoansError.InvalidTerm);
            }  //agregar _logger

            return ValidationResult.Success();
        }

        public async Task<ValidationResult> EditValidateAnnualInterestRateAsync(int Id)
        {
            var loan = await _loansRepository.GetByIdAsync(Id);
            if (loan is null)
            {
                _logger.LogWarning("Prestamo con el ID {ID} no encontrado", Id);
                return ValidationResult<EditAnnualInterestRateDto>.Failure(LoansError.NonExistsLoan);
            }

            if (loan.Status != LoanStatus.Activo)
            {
                _logger.LogWarning("Prestamo con el ID {ID} no posee un estado de consulta valido {Estado}",
                    loan.Id, loan.Status.ToString()
                  );
                return ValidationResult<EditAnnualInterestRateDto>.Failure(LoansError.LoanIsNotActive);
            }
            _logger.LogInformation("Prestamo con ID {ID} encontrado y valido para ser operado", loan.Id);
            return ValidationResult.Success();
        }

        public async Task<ValidationResult> GetLoansByCustomerValidateAsync(ConsultClientByIdCardDto customer)
        {
            if (customer is null) {

                _logger.LogWarning("Datos de consulta invalidos5");
                return ValidationResult.Failure(GeneralError.DataInvalid);
            }
            //agregar aqui consulta de existencia de un cliente por su cedula
            //services creado por adrian en espera de creacion

            //agregar aqui exitencia de prestamos de dicho usuario consultado por cedula
            //obtener datos del mismo con el return by id card de adrian, en espera de creacion
                
            return ValidationResult.Success();
        }

        public async Task<ValidationResult> GetLoansByStatusInCustomerValidateAsync(
            string customerId,
            LoanStatusFilter loanStatusFilter)
        {
            //agregar verificacion de existencia del cliente
            //con el exist del metodo de adrian, en espera de creacion

            _logger.LogInformation("Verificando existencia de prestamos del cliente con ID {ID}", customerId);
            var existLoansByUser = await _loansRepository.ExistElementByConsult(x => x.CustomerId == customerId);
            if (!existLoansByUser)
            {
                _logger.LogWarning("Existencia de prestamos del cliente con ID {ID} es inexistente", customerId);
                return ValidationResult.Failure(LoansError.NonExistsLoans);
            }

            _logger.LogInformation("Verificando existencia de prestamos del cliente con ID {ID}" +
                " por parte del estado indicado, estado {Status}", customerId, loanStatusFilter.ToString());
            if (loanStatusFilter != LoanStatusFilter.Todos) {
                LoanStatus value = 
                    loanStatusFilter == LoanStatusFilter.Activos
                    ? LoanStatus.Activo : LoanStatus.Completado;

                _logger.LogInformation("Consulta de la existencia de prestamos con el estado" +
                    " {Status} del cliente con ID {ID}", loanStatusFilter.ToString(), customerId);
                var existLoansByUserInByStatus = await
                    _loansRepository.ExistElementByConsult(x => x.CustomerId == customerId && x.Status == value);
                if (!existLoansByUserInByStatus)
                {
                    _logger.LogWarning("Inexistencia de prestamos con el estado {Status} para el cliente con ID {ID}",
                        loanStatusFilter.ToString(), customerId);
                    return ValidationResult.Failure(LoansError.NonExistLoansByIndicateState);
                }
            }

            return ValidationResult.Success();
        }

        //agregar private metodo que determine si el current user tiene Role -> Administrador
    }
}
