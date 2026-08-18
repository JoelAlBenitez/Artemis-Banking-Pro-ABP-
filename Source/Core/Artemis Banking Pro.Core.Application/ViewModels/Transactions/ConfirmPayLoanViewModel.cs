namespace Artemis_Banking_Pro.Core.Application.ViewModels.Transactions
{
    public class ConfirmPayLoanViewModel
    {
        public string SourceAccountNumber { get; set; } = null!;
        public string OriginOwnerName { get; set; } = null!;
        public int LoanId { get; set; }
        public string LoanNumber { get; set; } = null!;
        public string LoanOwnerName { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal EffectiveAmount { get; set; }
    }
}
