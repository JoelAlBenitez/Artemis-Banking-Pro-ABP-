using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Base;

namespace ArtemisBankingPro.Core.Domain.Entities.CreditCards
{
    public sealed class CreditCard : BaseEntitie<int>
    {
        public required string CardNumber { get; set; }
        public required string LastFourDigits { get; set; }
        public required string CustomerId { get; set; }
        public required decimal CreditLimit { get; set; }
        public decimal OwedAmount { get; set; }
        public required DateTimeOffset ExpirationDate { get; set; }
        public required string CvcHash { get; set; }
        public required CreditCardStatus Status { get; set; }
        public required string AssignedByAdminId { get; set; }
        public bool IsExpired => ExpirationDate <= DateTimeOffset.UtcNow;

        public decimal AvailableCredit => CreditLimit - OwedAmount;

        //Collections

        public ICollection<CardConsumption> cardConsumptions { get; set; } = new List<CardConsumption>();
    }
}
