namespace Artemis_Banking_Pro.Core.Application.DTOs.Transactions
{
    public sealed class CashAdvanceDto
    {
        public decimal RequestedAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalCharged { get; set; }
        public required string CardLastFourDigits { get; set; }
        public required string AccountLastFourDigits { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
