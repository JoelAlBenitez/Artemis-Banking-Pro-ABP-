using Artemis_Banking_Pro.Core.Application.ViewModels.Base;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Loans
{
    public sealed class LoansViewModel : BaseViewModel<int>
    {
        public required string LoanNumber { get; set; }
        public required string FullNameCustomer { get; set; }
        public required decimal AprovechedCapital { get; set; }
        public required int QuantityInstallment { get; set; }
        public required int InstallmentPay { get; set; }
        public required decimal PendientAmount { get; set; }
        public required decimal AnnualInterestRate { get; set; }
        public required int Term { get; set; }

        public required string StateLoans { get; set; }
        public required string StateCustomer { get; set; }

    }
}
