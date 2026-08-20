using Artemis_Banking_Pro.Core.Application.Contracts.Generic;
using Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Beneficiaries
{
    public interface IBeneficiaryServices : IGenericServices<SaveBeneficiaryDto, BeneficiaryDto, int>
    {
        Task<ValidationResult> DeactivateAsync(int id, string ownerClientId);
        Task<ValidationResult<IReadOnlyCollection<BeneficiaryDto>>> GetClientBeneficiariesAsync(string ownerClientId);
    }
}
