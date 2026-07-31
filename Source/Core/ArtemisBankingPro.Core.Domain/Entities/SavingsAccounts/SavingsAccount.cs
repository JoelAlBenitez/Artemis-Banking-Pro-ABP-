using ArtemisBankingPro.Core.Domain.Entities.Base;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;

namespace ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts
{
    public sealed class SavingsAccount : BaseEntitie<int>
    {
        public required string AccountNumber { get; set; }
        public required string ClientId { get; set; }
        public decimal Balance { get; set; }
        public required SavingsAccountType Type { get; set; }
        public required SavingsAccountStatus Status { get; set; }

        // Navigation properties
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
