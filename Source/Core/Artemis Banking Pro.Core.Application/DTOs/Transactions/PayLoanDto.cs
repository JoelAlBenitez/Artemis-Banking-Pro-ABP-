namespace Artemis_Banking_Pro.Core.Application.DTOs.Transactions
{
    public sealed class PayLoanDto
    {
        public required string SourceAccountNumber { get; set; }
        public int LoanId { get; set; }
        public decimal Amount { get; set; }
    }
}
