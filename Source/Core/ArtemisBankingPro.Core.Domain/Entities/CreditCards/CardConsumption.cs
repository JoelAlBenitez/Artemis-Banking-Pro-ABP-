using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Base;

namespace ArtemisBankingPro.Core.Domain.Entities.CreditCards
{
    public sealed class CardConsumption : BaseEntitie<int>
    {
        public required int CreditCardId { get; set; }
        public required decimal Amount { get; set; }
        public required ConsumptionOrigin Origin { get; set; }

        public int? CommerceId { get; set; }
        public required string CommerceName { get; set; }
        public required ConsumptionStatus Status { get; set; }
        public RejectionReason? RejectionReason { get; set; }

        public CreditCard CreditCard { get; set; } = null!;
    }
}
