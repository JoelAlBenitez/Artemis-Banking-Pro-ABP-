using Artemis_Banking_Pro.Core.Application.DTOs.Base;
using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.CreditCards
{
    public sealed class CardConsumptionDto : BaseDto<int>
    {
        public int CreditCardId { get; set; }
        public DateTimeOffset Date { get; set; }
        public decimal Amount { get; set; }
        public required string CommerceName { get; set; }
        public required CardConsumptionStatus Status { get; set; }
        public string? RejectionReason { get; set; }
    }
}
