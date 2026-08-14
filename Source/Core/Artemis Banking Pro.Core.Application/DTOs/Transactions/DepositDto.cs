namespace Artemis_Banking_Pro.Core.Application.DTOs.Transactions
{
    public sealed class DepositDto
    {
        public required string AccountNumber { get; set; }
        public required decimal Amount { get; set; }
        public required string CashierId { get; set; }
    }
}
