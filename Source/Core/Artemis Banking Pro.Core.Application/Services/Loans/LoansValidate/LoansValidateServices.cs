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
            var loan = await _loansRepository.GetFirstAsync(x => x.Id == Id);
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

            _logger.LogInformation("Recuperando las cuotas futuros del prestamo con ID {ID}", Id);
            var today = DateTimeOffset.UtcNow;
            var futureInstallments = loan.loanInstallments
                .Where(i => i.paymentStatus == PaymentStatus.Pendiente && i.DueDate > today)
                .ToList();
            if (futureInstallments.Count == 0)
            {
                _logger.LogWarning("Inexistencia de cutoas futurass del prestamo con ID {ID} ", Id);
                return ValidationResult<LoanRateUpdatedDto>.Failure(LoansError.NonExistsFutureInstallments);
            }
            _logger.LogInformation("Prestamo con ID {ID} encontrado y valido para ser operado", loan.Id);
            return ValidationResult.Success();
        }

        public async Task<ValidationResult> GetLoansByCustomerValidateAsync(LoansFilterDto dto)
        {

            if (!string.IsNullOrWhiteSpace(dto.IdCard))
            {
                //modificar cuando se integre el ICurrentUserServices por parte de adrian 
                //agregar aqui obtencion de cliente por su cedula obtener cliente y proceder con las consultas por su id asociado

                _logger.LogInformation("Verificando existencia de prestamos del cliente con la cédula {ID}", 0);
                var existLoansByUser = await _loansRepository.ExistElementByConsult(x => x.CustomerId == "");
                if (!existLoansByUser)
                {
                    _logger.LogWarning("Existencia de prestamos del cliente con ID {ID} es inexistente", 0);
                    return ValidationResult.Failure(LoansError.NonExistsLoans);
                }

                //por modificar ICurrentUserServices pendiente | en espera
                _logger.LogInformation("Verificando existencia de prestamos del cliente con ID {ID}" +
                    " por parte del estado indicado, estado {Status}", 0, "");
                if (dto.Status != LoanStatusFilter.Todos)
                {
                    LoanStatus value =
                        dto.Status == LoanStatusFilter.Activos
                        ? LoanStatus.Activo : LoanStatus.Completado;

                    _logger.LogInformation("Consulta de la existencia de prestamos con el estado" +
                        " {Status} del cliente con ID {ID}", dto.Status.ToString(), 0); //por modificar
                    var existLoansByUserInByStatus = await
                        _loansRepository.ExistElementByConsult(x => x.CustomerId == "" && x.Status == value); //por modificar 
                    if (!existLoansByUserInByStatus)
                    {
                        _logger.LogWarning("Inexistencia de prestamos con el estado {Status} para el cliente con ID {ID}",
                            dto.Status.ToString(), 0); //por modificar
                        return ValidationResult.Failure(LoansError.NonExistLoansByIndicateState);
                    }

                }
            }

            return ValidationResult.Success();
        }

      

        //agregar private metodo que determine si el current user tiene Role -> Administrador
    }
}
