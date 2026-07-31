using ArtemisBankingPro.Core.Domain.Entities.Base;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;

namespace ArtemisBankingPro.Core.Domain.Entities.CreditCards
{
    public sealed class CardPayment : BaseEntitie<int>
    {
        public required int CreditCardId { get; set; }
        public required int TransactionId { get; set; }
        public required decimal RequestedAmount { get; set; }
        public required decimal EffectiveAmount { get; set; }
        public required ChannelPayment Channel { get; set; }
        public required string PerformedByUserId { get; set; }

        // Navigation properties
        public CreditCard? CreditCard { get; set; } = null;
        public Transaction? Transaction { get; set; } = null;
    }
}
