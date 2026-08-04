namespace Artemis_Banking_Pro.Core.Application.DTOs.Transactions
{
    public sealed class TransactionResultDto
    {
        public decimal EffectiveAmount { get; set; }
        public required string TransactionType { get; set; }
        public required string Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? WarningMessage { get; set; }
    }
}
