namespace Artemis_Banking_Pro.Core.Application.DTOs.Transactions
{
    public sealed class InitialTransactionDto
    {
        public int SavingsAccountId { get; set; }
        public decimal Amount { get; set; }
        public required string PerformedByUserId { get; set; }
    }
}
