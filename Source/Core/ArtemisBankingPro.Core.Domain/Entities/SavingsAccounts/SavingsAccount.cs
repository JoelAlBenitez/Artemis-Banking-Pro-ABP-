using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Base;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;

namespace ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts
{
    public sealed class SavingsAccount : BaseEntitie<int>
    {
       
        public required string AccountNumber { get; set; }

        public required string CustomerId { get; set; }

        public decimal Balance { get; set; }

        public required SavingsAccountType AccountType { get; set; }

        public required SavingsAccountStatus Status { get; set; }

        public DateTimeOffset? StatusChangedAt { get; set; }

        public bool IsPrimary => AccountType == SavingsAccountType.Principal;

        public bool IsActive => Status == SavingsAccountStatus.Activa;

        //Collections

        //Extremo inverso de la relación configurada en TransactionConfiguration. EF Core
        //necesita una colección mutable para materializar la navegación, por lo que no puede
        //declararse como IReadOnlyCollection.
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
