using ArtemisBankingPro.Core.Domain.Entities.Base;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;

namespace ArtemisBankingPro.Core.Domain.Entities.Transactions
{
    public sealed class Transaction : BaseEntitie<int>
    {
        public required int SavingsAccountId { get; set; }
        public required decimal Amount { get; set; }
        public required TransactionType TransactionType { get; set; }
        public required OperationType OperationType { get; set; }
        public required string Origin { get; set; }
        public string? Beneficiary { get; set; }
        public required TransactionStatus Status { get; set; }
        public string? RejectionReason { get; set; }
        public required string PerformedByUserId { get; set; }
        public required ChannelPayment Channel { get; set; }
        public int? RelatedTransactionId { get; set; }

        // Navigation properties
        public SavingsAccount? SavingsAccount { get; set; } = null;
        public Transaction? RelatedTransaction { get; set; } = null;
    }
}
