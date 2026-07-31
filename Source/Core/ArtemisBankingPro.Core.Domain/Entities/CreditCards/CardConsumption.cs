using ArtemisBankingPro.Core.Domain.Entities.Base;
using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace ArtemisBankingPro.Core.Domain.Entities.CreditCards
{
    public sealed class CardConsumption : BaseEntitie<int>
    {
        public required int CreditCardId { get; set; }
        public required DateTimeOffset Date { get; set; }
        public required decimal Amount { get; set; }
        public required string CommerceName { get; set; }
        public required CardConsumptionStatus Status { get; set; }
        public string? RejectionReason { get; set; }

        // Navigation properties
        public CreditCard? CreditCard { get; set; }
    }
}
