namespace Artemis_Banking_Pro.Core.Application.DTOs.Transactions
{
    public sealed class TransactionIndicatorsDto
    {
        public int TotalTransactions { get; set; }
        public decimal TotalPaymentsAmount { get; set; }
        public decimal TotalDepositsAmount { get; set; }
        public decimal TotalWithdrawalsAmount { get; set; }
    }
}
