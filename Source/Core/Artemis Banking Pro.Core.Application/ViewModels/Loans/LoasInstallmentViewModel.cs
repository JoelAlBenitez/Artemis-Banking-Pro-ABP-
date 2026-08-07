namespace Artemis_Banking_Pro.Core.Application.ViewModels.Loans
{
    public sealed class LoasInstallmentViewModel
    {
        public required int NumberLoanInstallment { get; set; }
        public required DateTimeOffset DueDate { get; set; }
        public required decimal InstallmentValue { get; set; }
        public required decimal OutstandingBalance { get; set; }
        public required string StateInstallment { get; set; }
        public required bool IsOverdue { get; set; }

    }
}
