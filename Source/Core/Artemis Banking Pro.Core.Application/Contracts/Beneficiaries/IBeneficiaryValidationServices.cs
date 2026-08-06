using Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Beneficiaries
{
    public interface IBeneficiaryValidationServices
    {
        Task<ValidationResult<SavingsAccount>> ValidateCreationAsync(SaveBeneficiaryDto dto);
        Task<ValidationResult<Beneficiary>> ValidateDeactivationAsync(int id, string ownerClientId);
    }
}
