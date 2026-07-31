using ArtemisBankingPro.Core.Domain.Entities.Base;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;

namespace ArtemisBankingPro.Core.Domain.Entities.Beneficiaries
{
    public sealed class Beneficiary : BaseEntitie<int>
    {
        public required string OwnerClientId { get; set; }
        public required int BeneficiarySavingsAccountId { get; set; }
        public required string BeneficiaryAccountNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset? DeactivatedAt { get; set; }

        // Navigation properties
        public SavingsAccount? BeneficiarySavingsAccount { get; set; }
    }
}
