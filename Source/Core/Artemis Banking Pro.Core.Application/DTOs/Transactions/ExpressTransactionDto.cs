namespace Artemis_Banking_Pro.Core.Application.DTOs.Transactions
{
    public sealed class ExpressTransactionDto
    {
        public required string SourceAccountNumber { get; set; }
        public required string DestinationAccountNumber { get; set; }
        public decimal Amount { get; set; }
    }
}
