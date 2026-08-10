using Artemis_Banking_Pro.Core.Application.Contracts.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.Services.Generic;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Interfaces.Beneficiaries;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.Beneficiaries
{
    public sealed class BeneficiaryServices
        : GenericServices<SaveBeneficiaryDto, BeneficiaryDto, int, Beneficiary>,
          IBeneficiaryServices
    {
        private readonly IBeneficiaryValidationServices _validationServices;
        private readonly ILogger<BeneficiaryServices> _logger;

        public BeneficiaryServices(
            IBeneficiaryRepository beneficiaryRepository,
            IBeneficiaryValidationServices validationServices,
            IMapper mapper,
            ILogger<BeneficiaryServices> logger)
            : base(beneficiaryRepository, mapper, logger)
        {
            _validationServices = validationServices;
            _logger = logger;
        }

        public override async Task<ValidationResult> CreateAsync(SaveBeneficiaryDto dto)
        {
            _logger.LogInformation("Iniciando registro de nuevo beneficiario para el cliente {ClientId} con cuenta {AccountNumber}", dto.OwnerClientId, dto.AccountNumber);

            var validation = await _validationServices.ValidateCreationAsync(dto);
            if (!validation.IsValid)
            {
                return ValidationResult.Failure(validation.Errors.ToList());
            }

            try
            {
                var savingsAccount = validation.Value!;
                var beneficiary = _mapper.Map<Beneficiary>(dto);
                beneficiary.BeneficiarySavingsAccountId = savingsAccount.Id;
                beneficiary.IsActive = true;
                beneficiary.CreateByUserId = dto.OwnerClientId;
                beneficiary.CreatedAt = DateTimeOffset.UtcNow;

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

            var validationResult = await _validationServices.ValidateDeactivationAsync(id, ownerClientId);
            if (!validationResult.IsValid)
            {
                return ValidationResult.Failure(validationResult.Errors.ToList());
            }

            try
            {
                var beneficiary = validationResult.Value!;
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

                var dtos = _mapper.Map<IReadOnlyCollection<BeneficiaryDto>>(beneficiaries);

                _logger.LogInformation("Se recuperaron {Count} beneficiarios activos para el cliente {ClientId}", dtos.Count, ownerClientId);
                return ValidationResult<IReadOnlyCollection<BeneficiaryDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico inesperado al obtener beneficiarios para el cliente {ClientId}", ownerClientId);
                return ValidationResult<IReadOnlyCollection<BeneficiaryDto>>.Failure(GeneralError.UnexpectedError);
            }
        }
    }
}
