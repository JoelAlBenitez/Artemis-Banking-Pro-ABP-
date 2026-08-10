namespace Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries
{
    public sealed class SaveBeneficiaryDto
    {
        public required string OwnerClientId { get; set; }
        public required string AccountNumber { get; set; }
    }
}
