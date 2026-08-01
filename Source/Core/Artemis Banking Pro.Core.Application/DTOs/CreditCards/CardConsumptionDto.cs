using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.CreditCards
{
    public sealed class CardConsumptionDto
    {
        public required DateTimeOffset ConsumptionDate { get; set; }
        public required decimal Amount { get; set; }
        public required string CommerceName { get; set; }
        public required ConsumptionStatus StateConsumption { get; set; }
    }
}
