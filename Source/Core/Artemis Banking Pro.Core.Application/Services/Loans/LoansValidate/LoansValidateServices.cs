using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.CodeErrors.LoansErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.Loans.LoansValidate
{

    public sealed class LoansValidateServices : ILoansValidateServices
    {
        private readonly ILoansRepository _loansRepository;
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly ILogger<LoansValidateServices> _logger;

        //integrar el ICurrentUserServices para el administrador responsable y su rol

        public LoansValidateServices(
            ILogger<LoansValidateServices> logger,
            ILoansRepository loansRepository,
            ISavingsAccountsRepository savingsAccountsRepository)
        {
            _loansRepository = loansRepository;
            _savingsAccountsRepository = savingsAccountsRepository;
            _logger = logger;
        }

        public async Task<ValidationResult> AssigmentLoansValidateAsync(LoansAssignmentDto assignment)
        {
            if (assignment is null)
            {
                _logger.LogWarning("Datos de asignacion invalidos");
                return ValidationResult.Failure(GeneralError.DataInvalid);
            }

            if (string.IsNullOrWhiteSpace(assignment.CustomerId))
            {
                _logger.LogWarning("Intento de asignacion de prestamo sin cliente seleccionado");
                return ValidationResult.Failure(LoansError.NonSelectedCustomer);
            }

            try
            {
                //La existencia del cliente y su estado activo se validan contra el project Identity.
                //Cuando el servicio de consulta de usuarios esté disponible, esta validación debe
                //consultarlo y agregar LoansError.NonExistsCustomerByIdCard o LoansError.CustomerIsNotActive.
                //var customer = await _userServices.GetCustomerByIdAsync(assignment.CustomerId);
                //if (customer is null) return ValidationResult.Failure(LoansError.NonExistsCustomerByIdCard);
                //if (!customer.IsActive) return ValidationResult.Failure(LoansError.CustomerIsNotActive);

                _logger.LogInformation("Verificaciones de prestamos activos del cliente con ID {ID}", assignment.CustomerId);
                var exist = await _loansRepository.ExistElementByConsult(x => x.CustomerId == assignment.CustomerId
                    && x.Status == LoanStatus.Activo);

                if (exist)
                {
                    _logger.LogWarning("Cliente con ID {ID} ya posee un prestamo activo", assignment.CustomerId);
                    return ValidationResult.Failure(LoansError.CustomerWithLoanExist);
                }

                var errors = new List<Error>();

                //Plazos permitidos: 6 a 60 meses en intervalos de 6
                var term = (int)assignment.TermLoans;
                if (term < 6 || term > 60 || !Enum.IsDefined(typeof(TermMonths), assignment.TermLoans))
                {
                    _logger.LogWarning("Plazo {Plazo} invalido para el prestamo del cliente con ID {ID}",
                        term, assignment.CustomerId);

                    errors.Add(LoansError.InvalidTerm);
                }

                if (assignment.AmmountLoans <= 0m)
                {
                    _logger.LogWarning("Monto {Monto} invalido para el prestamo del cliente con ID {ID}",
                        assignment.AmmountLoans, assignment.CustomerId);

                    errors.Add(LoansError.InvalidAmount);
                }

                if (assignment.AnnualInterestRate < 0m)
                {
                    _logger.LogWarning("Tasa {Tasa} negativa para el prestamo del cliente con ID {ID}",
                        assignment.AnnualInterestRate, assignment.CustomerId);

                    errors.Add(LoansError.NegativeAnnualInterestRate);
                }

                if (errors.Count > 0)
                {
                    return ValidationResult.Failure(errors);
                }

                //Sin cuenta principal activa la operación no debe iniciarse: no habría dónde
                //desembolsar el capital aprobado.
                var hasActivePrimaryAccount = await _savingsAccountsRepository.ExistElementByConsult(
                    account => account.CustomerId == assignment.CustomerId
                        && account.AccountType == SavingsAccountType.Principal
                        && account.Status == SavingsAccountStatus.Activa);

                if (!hasActivePrimaryAccount)
                {
                    _logger.LogWarning("El cliente con ID {ID} no posee una cuenta de ahorro principal activa receptora",
                        assignment.CustomerId);

                    return ValidationResult.Failure(LoansError.NonExistAccountFirstActive);
                }

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar la asignacion del prestamo al cliente con ID {ID}",
                    assignment.CustomerId);

                return ValidationResult.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<Loan>> EditValidateAnnualInterestRateAsync(int Id)
        {
            try
            {
                //Sin las cuotas la colección llega vacía y ninguna cuota futura sería visible
                var loan = await _loansRepository.GetFirstAsync(x => x.Id == Id, x => x.loanInstallments);

                if (loan is null)
                {
                    _logger.LogWarning("Prestamo con el ID {ID} no encontrado", Id);
                    return ValidationResult<Loan>.Failure(LoansError.NonExistsLoan);
                }

                if (loan.Status != LoanStatus.Activo)
                {
                    _logger.LogWarning("Prestamo con el ID {ID} no posee un estado de consulta valido {Estado}",
                        loan.Id, loan.Status.ToString());

                    return ValidationResult<Loan>.Failure(LoansError.LoanIsNotActive);
                }

                _logger.LogInformation("Recuperando las cuotas futuras del prestamo con ID {ID}", Id);
                var today = DateTimeOffset.UtcNow;
                var futureInstallments = loan.loanInstallments
                    .Where(i => i.paymentStatus == PaymentStatus.Pendiente && i.DueDate > today)
                    .ToList();

                if (futureInstallments.Count == 0)
                {
                    _logger.LogWarning("Inexistencia de cuotas futuras del prestamo con ID {ID}", Id);
                    return ValidationResult<Loan>.Failure(LoansError.NonExistsFutureInstallments);
                }

                _logger.LogInformation("Prestamo con ID {ID} encontrado y valido para ser operado", loan.Id);
                return ValidationResult<Loan>.Success(loan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar la edicion de tasa del prestamo con ID {ID}", Id);
                return ValidationResult<Loan>.Failure(GeneralError.UnexpectedError);
            }
        }

        public Task<ValidationResult> GetLoansByCustomerValidateAsync(LoansFilterDto dto)
        {
            if (dto is null)
            {
                _logger.LogWarning("Filtros de consulta de prestamos invalidos");
                return Task.FromResult(ValidationResult.Failure(GeneralError.DataInvalid));
            }

            if (string.IsNullOrWhiteSpace(dto.IdCard))
            {
                return Task.FromResult(ValidationResult.Success());
            }

            //La cédula identifica al cliente dentro del project Identity. Cuando el servicio de
            //consulta de usuarios esté disponible, esta validación debe traducirla a su ID y
            //devolver LoansError.NonExistsCustomerByIdCard cuando no exista. La inexistencia de
            //préstamos del cliente se resuelve en el servicio, ya con el listado en la mano.
            //var customer = await _userServices.GetCustomerByIdCardAsync(dto.IdCard);
            //if (customer is null) return ValidationResult.Failure(LoansError.NonExistsCustomerByIdCard);

            _logger.LogInformation("Consulta de prestamos por la cedula {IdCard}", dto.IdCard);

            return Task.FromResult(ValidationResult.Success());
        }
    }
}