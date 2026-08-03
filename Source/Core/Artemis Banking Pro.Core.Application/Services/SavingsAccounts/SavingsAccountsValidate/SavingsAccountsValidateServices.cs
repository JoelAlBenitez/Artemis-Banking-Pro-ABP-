using Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.CodeErrors.SavingsAccountsErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.SavingsAccounts.SavingsAccountsValidate
{
    //Único lugar donde viven las reglas de negocio del módulo de cuentas de ahorro.
    //Las validaciones se ejecutan siempre antes de escribir en la base de datos.
    public sealed class SavingsAccountsValidateServices : ISavingsAccountsValidateServices
    {
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly ILogger<SavingsAccountsValidateServices> _logger;

        //integrar el ICurrentUserServices para el administrador responsable y su rol

        public SavingsAccountsValidateServices(
            ISavingsAccountsRepository savingsAccountsRepository,
            ILogger<SavingsAccountsValidateServices> logger)
        {
            _savingsAccountsRepository = savingsAccountsRepository;
            _logger = logger;
        }

        public async Task<ValidationResult> ValidateCustomerSelectionAsync(string customerId)
        {
            _logger.LogInformation("Validando el cliente {CustomerId} seleccionado para asignarle una cuenta de ahorro secundaria",
                customerId);

            if (string.IsNullOrWhiteSpace(customerId))
            {
                _logger.LogWarning("Intento de asignación de cuenta de ahorro sin cliente seleccionado");
                return ValidationResult.Failure(SavingsAccountError.CustomerRequired);
            }

            try
            {
                //La existencia del cliente y su estado activo se validan contra el project Identity.
                //Cuando el servicio de consulta de usuarios esté disponible, esta validación debe consultarlo
                //y agregar SavingsAccountError.NonExistsCustomerByIdCard o SavingsAccountError.CustomerIsNotActive.
                //var customer = await _userServices.GetCustomerByIdAsync(customerId);
                //if (customer is null) return ValidationResult.Failure(SavingsAccountError.NonExistsCustomerByIdCard);
                //if (!customer.IsActive) return ValidationResult.Failure(SavingsAccountError.CustomerIsNotActive);

                //La cuenta principal activa sí pertenece a este módulo y se verifica aquí.
                //Basta con saber si existe: no se materializa la fila.
                var hasActivePrimaryAccount = await _savingsAccountsRepository.ExistElementByConsult(
                    account => account.CustomerId == customerId
                        && account.AccountType == SavingsAccountType.Principal
                        && account.Status == SavingsAccountStatus.Activa);

                if (!hasActivePrimaryAccount)
                {
                    _logger.LogWarning("El cliente {CustomerId} no posee una cuenta de ahorro principal activa",
                        customerId);

                    return ValidationResult.Failure(SavingsAccountError.CustomerWithoutActivePrimaryAccount);
                }

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar el cliente {CustomerId} para la asignación de una cuenta de ahorro",
                    customerId);

                return ValidationResult.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult> ValidateAssignmentAsync(SavingsAccountAssignmentDto dto)
        {
            if (dto is null)
            {
                _logger.LogWarning("Datos de asignación de cuenta de ahorro inválidos");
                return ValidationResult.Failure(GeneralError.DataInvalid);
            }

            _logger.LogInformation("Validando la asignación de una cuenta de ahorro secundaria al cliente {CustomerId}",
                dto.CustomerId);

            var customerValidation = await ValidateCustomerSelectionAsync(dto.CustomerId);
            if (!customerValidation.IsValid)
            {
                return customerValidation;
            }

            var errors = new List<Error>();

            //El balance inicial puede ser RD$0.00, pero nunca negativo
            if (dto.InitialBalance < 0m)
            {
                errors.Add(SavingsAccountError.NegativeInitialBalance);
            }

            if (errors.Count > 0)
            {
                _logger.LogWarning("Asignación de cuenta de ahorro rechazada para el cliente {CustomerId}. Reglas incumplidas: {Reglas}",
                    dto.CustomerId, errors.Select(e => e.Description));

                return ValidationResult.Failure(errors);
            }

            return ValidationResult.Success();
        }

        public async Task<ValidationResult<SavingsAccount>> ValidateActiveSavingsAccountAsync(int savingsAccountId)
        {
            try
            {
                _logger.LogInformation("Validando la existencia y el estado de la cuenta de ahorro con ID {SavingsAccountId}",
                    savingsAccountId);

                var savingsAccount = await _savingsAccountsRepository.GetByIdAsync(savingsAccountId);

                if (savingsAccount is null)
                {
                    _logger.LogWarning("Cuenta de ahorro con ID {SavingsAccountId} inexistente", savingsAccountId);
                    return ValidationResult<SavingsAccount>.Failure(SavingsAccountError.NonExistsSavingsAccount);
                }

                if (savingsAccount.Status != SavingsAccountStatus.Activa)
                {
                    _logger.LogWarning("La cuenta de ahorro {AccountNumber} ya se encuentra cancelada",
                        savingsAccount.AccountNumber);

                    return ValidationResult<SavingsAccount>.Failure(SavingsAccountError.SavingsAccountAlreadyCancelled);
                }

                return ValidationResult<SavingsAccount>.Success(savingsAccount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar la cuenta de ahorro con ID {SavingsAccountId}", savingsAccountId);
                return ValidationResult<SavingsAccount>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<SavingsAccount>> ValidateCancellationAsync(int savingsAccountId)
        {
            _logger.LogInformation("Validando la cancelación de la cuenta de ahorro con ID {SavingsAccountId}",
                savingsAccountId);

            var accountValidation = await ValidateActiveSavingsAccountAsync(savingsAccountId);
            if (!accountValidation.IsValid)
            {
                return accountValidation;
            }

            var savingsAccount = accountValidation.Value!;

            //Las principales no muestran la acción Cancelar y tampoco pueden cancelarse por URL
            if (savingsAccount.IsPrimary)
            {
                _logger.LogWarning("Cancelación rechazada: la cuenta {AccountNumber} es principal",
                    savingsAccount.AccountNumber);

                return ValidationResult<SavingsAccount>.Failure(SavingsAccountError.PrimaryAccountCannotBeCancelled);
            }

            try
            {
                //Debe existir una principal activa que reciba el balance remanente
                var hasActivePrimaryAccount = await _savingsAccountsRepository.ExistElementByConsult(
                    account => account.CustomerId == savingsAccount.CustomerId
                        && account.AccountType == SavingsAccountType.Principal
                        && account.Status == SavingsAccountStatus.Activa);

                if (!hasActivePrimaryAccount)
                {
                    _logger.LogWarning("Cancelación rechazada: el cliente {CustomerId} no tiene una cuenta principal activa receptora",
                        savingsAccount.CustomerId);

                    return ValidationResult<SavingsAccount>.Failure(SavingsAccountError.WithoutPrimaryAccountToReceiveFunds);
                }

                return ValidationResult<SavingsAccount>.Success(savingsAccount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar la cancelación de la cuenta de ahorro con ID {SavingsAccountId}",
                    savingsAccountId);

                return ValidationResult<SavingsAccount>.Failure(GeneralError.UnexpectedError);
            }
        }

        public Task<ValidationResult> ValidateCustomerAccountsQueryAsync(SavingsAccountFilterDto filter)
        {
            if (filter is null)
            {
                _logger.LogWarning("Filtros de consulta de cuentas de ahorro inválidos");
                return Task.FromResult(ValidationResult.Failure(GeneralError.DataInvalid));
            }

            if (string.IsNullOrWhiteSpace(filter.IdCard))
            {
                return Task.FromResult(ValidationResult.Success());
            }

            //La cédula identifica al cliente dentro del project Identity. Cuando el servicio de
            //consulta de usuarios esté disponible, esta validación debe traducirla a su ID y
            //devolver SavingsAccountError.NonExistsCustomerByIdCard cuando no exista.
            //var customer = await _userServices.GetCustomerByIdCardAsync(filter.IdCard);
            //if (customer is null) return ValidationResult.Failure(SavingsAccountError.NonExistsCustomerByIdCard);

            _logger.LogInformation("Consulta de cuentas de ahorro por la cédula {IdCard}", filter.IdCard);

            return Task.FromResult(ValidationResult.Success());
        }
    }
}
