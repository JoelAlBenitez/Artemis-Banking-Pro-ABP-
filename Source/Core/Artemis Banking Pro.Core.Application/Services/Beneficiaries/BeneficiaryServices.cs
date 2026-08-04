using Artemis_Banking_Pro.Core.Application.Contracts.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.Services.Generic;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.Beneficiaries
{
    public sealed class BeneficiaryServices
        : GenericServices<SaveBeneficiaryDto, BeneficiaryDto, int, Beneficiary>,
          IBeneficiaryServices
    {
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly ILogger<BeneficiaryServices> _logger;

        public BeneficiaryServices(
            IBeneficiaryRepository beneficiaryRepository,
            ISavingsAccountsRepository savingsAccountsRepository,
            IMapper mapper,
            ILogger<BeneficiaryServices> logger)
            : base(beneficiaryRepository, mapper, logger)
        {
            _savingsAccountsRepository = savingsAccountsRepository;
            _logger = logger;
        }

        public override async Task<ValidationResult> CreateAsync(SaveBeneficiaryDto dto)
        {
            _logger.LogInformation("Iniciando registro de nuevo beneficiario para el cliente {ClientId} con cuenta {AccountNumber}", dto.OwnerClientId, dto.AccountNumber);

            var validation = await ValidateBeneficiaryCreationAsync(dto);
            if (!validation.IsValid)
            {
                return ValidationResult.Failure(validation.Errors.ToList());
            }

            try
            {
                var savingsAccount = validation.Value!;
                var beneficiary = new Beneficiary
                {
                    OwnerClientId = dto.OwnerClientId,
                    BeneficiarySavingsAccountId = savingsAccount.Id,
                    BeneficiaryAccountNumber = dto.AccountNumber,
                    IsActive = true,
                    CreateByUserId = dto.OwnerClientId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _genericRepository.AddAsync(beneficiary);
                var saveResult = await _genericRepository.SaveChangesAsync();
                if (saveResult <= 0)
                {
                    _logger.LogWarning("Error de persistencia: no se pudo guardar el beneficiario para el cliente {ClientId}", dto.OwnerClientId);
                    return ValidationResult.Failure(GeneralError.UnexpectedError);
                }

                _logger.LogInformation("Beneficiario registrado con éxito para el cliente {ClientId} con cuenta {AccountNumber}", dto.OwnerClientId, dto.AccountNumber);
                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico inesperado al registrar beneficiario para el cliente {ClientId}", dto.OwnerClientId);
                return ValidationResult.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult> DeactivateAsync(int id, string ownerClientId)
        {
            _logger.LogInformation("Iniciando baja lógica de beneficiario ID {BeneficiaryId} para el cliente {ClientId}", id, ownerClientId);

            try
            {
                var beneficiary = await _genericRepository.GetFirstAsync(b => b.Id == id && b.OwnerClientId == ownerClientId);
                if (beneficiary is null)
                {
                    _logger.LogWarning("Baja lógica fallida: el beneficiario ID {BeneficiaryId} no existe o no pertenece al cliente {ClientId}", id, ownerClientId);
                    return ValidationResult.Failure(BeneficiaryError.BeneficiaryNotFound);
                }

                beneficiary.IsActive = false;
                beneficiary.DeactivatedAt = DateTimeOffset.UtcNow;
                beneficiary.LastModifiedByIdUser = ownerClientId;
                beneficiary.ModifiedAt = DateTimeOffset.UtcNow;

                await _genericRepository.UpdateAsync(beneficiary);
                var saveResult = await _genericRepository.SaveChangesAsync();
                if (saveResult <= 0)
                {
                    _logger.LogWarning("Error de persistencia: no se pudo guardar la desactivación del beneficiario ID {BeneficiaryId}", id);
                    return ValidationResult.Failure(GeneralError.UnexpectedError);
                }

                _logger.LogInformation("Beneficiario ID {BeneficiaryId} desactivado correctamente", id);
                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico inesperado al desactivar el beneficiario ID {BeneficiaryId} para el cliente {ClientId}", id, ownerClientId);
                return ValidationResult.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<IReadOnlyCollection<BeneficiaryDto>>> GetClientBeneficiariesAsync(string ownerClientId)
        {
            _logger.LogInformation("Recuperando listado de beneficiarios activos para el cliente {ClientId}", ownerClientId);

            try
            {
                var beneficiaries = await _genericRepository.GetAllFindAsync(
                    b => b.OwnerClientId == ownerClientId && b.IsActive,
                    b => b.BeneficiarySavingsAccount!
                );

                var dtos = beneficiaries.Select(b => new BeneficiaryDto
                {
                    Id = b.Id,
                    AccountNumber = b.BeneficiaryAccountNumber,
                    OwnerFullName = b.BeneficiarySavingsAccount is not null 
                        ? $"Cliente {b.BeneficiarySavingsAccount.CustomerId}" 
                        : "Cliente Desconocido"
                }).ToList();

                _logger.LogInformation("Se recuperaron {Count} beneficiarios activos para el cliente {ClientId}", dtos.Count, ownerClientId);
                return ValidationResult<IReadOnlyCollection<BeneficiaryDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico inesperado al obtener beneficiarios para el cliente {ClientId}", ownerClientId);
                return ValidationResult<IReadOnlyCollection<BeneficiaryDto>>.Failure(GeneralError.UnexpectedError);
            }
        }

        #region Helper Methods

        private async Task<ValidationResult<SavingsAccount>> ValidateBeneficiaryCreationAsync(SaveBeneficiaryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.AccountNumber) || dto.AccountNumber.Length != 9 || !dto.AccountNumber.All(char.IsDigit))
            {
                _logger.LogWarning("Validación fallida: el número de cuenta '{AccountNumber}' no tiene el formato válido de 9 dígitos", dto.AccountNumber);
                return ValidationResult<SavingsAccount>.Failure(BeneficiaryError.AccountNotFound);
            }

            var savingsAccount = await _savingsAccountsRepository.GetFirstAsync(a => a.AccountNumber == dto.AccountNumber);
            if (savingsAccount is null)
            {
                _logger.LogWarning("Validación fallida: la cuenta de ahorros {AccountNumber} no existe", dto.AccountNumber);
                return ValidationResult<SavingsAccount>.Failure(BeneficiaryError.AccountNotFound);
            }

            if (savingsAccount.Status != SavingsAccountStatus.Activa)
            {
                _logger.LogWarning("Validación fallida: la cuenta de ahorros {AccountNumber} se encuentra cancelada o inactiva", dto.AccountNumber);
                return ValidationResult<SavingsAccount>.Failure(BeneficiaryError.AccountCanceled);
            }

            if (savingsAccount.CustomerId == dto.OwnerClientId)
            {
                _logger.LogWarning("Validación fallida: el cliente {ClientId} intentó agregarse a sí mismo como beneficiario", dto.OwnerClientId);
                return ValidationResult<SavingsAccount>.Failure(BeneficiaryError.OwnAccount);
            }

            var alreadyExists = await _genericRepository.ExistElementByConsult(b => 
                b.OwnerClientId == dto.OwnerClientId && 
                b.BeneficiarySavingsAccountId == savingsAccount.Id && 
                b.IsActive);

            if (alreadyExists)
            {
                _logger.LogWarning("Validación fallida: la cuenta {AccountNumber} ya se encuentra registrada como beneficiario activo para el cliente {ClientId}", dto.AccountNumber, dto.OwnerClientId);
                return ValidationResult<SavingsAccount>.Failure(BeneficiaryError.AlreadyRegistered);
            }

            return ValidationResult<SavingsAccount>.Success(savingsAccount);
        }

        #endregion
    }
}
