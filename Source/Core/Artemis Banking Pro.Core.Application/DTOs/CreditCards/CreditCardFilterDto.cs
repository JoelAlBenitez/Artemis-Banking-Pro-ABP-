using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.CreditCards
{
    public sealed class CreditCardFilterDto
    {
        public string? IdCard { get; set; }
        public CreditCardStatusFilter Status { get; set; } = CreditCardStatusFilter.Activas;
        public int Page { get; set; } = 1;
    }
}
