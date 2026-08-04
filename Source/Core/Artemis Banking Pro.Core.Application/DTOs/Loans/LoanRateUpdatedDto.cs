namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    public sealed class LoanRateUpdatedDto
    {
        public required string CustomerId { get; set; }
        public required string LoanNumber { get; set; }
        public required decimal AnnualInterestRate { get; set; }
        public required decimal NextInstallmentValue { get; set; }
        public required DateTimeOffset NextInstallmentDueDate { get; set; }
    }
}
