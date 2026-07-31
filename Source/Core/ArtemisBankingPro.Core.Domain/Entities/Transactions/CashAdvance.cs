using ArtemisBankingPro.Core.Domain.Entities.Base;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;

namespace ArtemisBankingPro.Core.Domain.Entities.Transactions
{
    public sealed class CashAdvance : BaseEntitie<int>
    {
        public required int CreditCardId { get; set; }
        public required int SavingsAccountId { get; set; }
        public required decimal RequestedAmount { get; set; }
        public required decimal InterestRate { get; set; }
        public required decimal InterestAmount { get; set; }
        public required decimal TotalCharged { get; set; }
        public required int CardConsumptionId { get; set; }
        public required int TransactionId { get; set; }

        // Navigation properties
        public CreditCard? CreditCard { get; set; }
        public SavingsAccount? SavingsAccount { get; set; }
        public CardConsumption? CardConsumption { get; set; }
        public Transaction? Transaction { get; set; }
    }
}
