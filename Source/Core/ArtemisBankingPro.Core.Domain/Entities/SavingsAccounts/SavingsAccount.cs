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

        //El historial de transacciones pertenece al módulo Cliente: ni la entidad ni su
        //repositorio se desarrollan aquí. Se habilita cuando ese módulo la exponga.
        public IReadOnlyCollection<Transaction>? Transactions { get; set; } = null;
    }
}
