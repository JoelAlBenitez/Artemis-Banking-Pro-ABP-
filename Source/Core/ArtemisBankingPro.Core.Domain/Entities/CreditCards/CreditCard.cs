using ArtemisBankingPro.Core.Domain.Entities.Base;
using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace ArtemisBankingPro.Core.Domain.Entities.CreditCards
{
    public sealed class CreditCard : BaseEntitie<int>
    {
        public required string CardNumber { get; set; }
        public required string CvcHash { get; set; }
        public required string ExpirationDate { get; set; }
        public required string ClientId { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal OwedAmount { get; set; }
        public required CreditCardStatus Status { get; set; }

        // Navigation properties
        public IReadOnlyCollection<CardConsumption>? Consumptions { get; set; } = null;
    }
}
