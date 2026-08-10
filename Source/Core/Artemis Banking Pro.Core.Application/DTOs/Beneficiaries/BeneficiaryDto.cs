using Artemis_Banking_Pro.Core.Application.DTOs.Base;

namespace Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries
{
    public sealed class BeneficiaryDto : BaseDto<int>
    {
        public required string AccountNumber { get; set; }
        public required string OwnerFullName { get; set; }
    }
}
