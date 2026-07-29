using Artemis_Banking_Pro.Core.Application.ViewModels.Base;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Loans
{
    public sealed class LoansViewModel : BaseViewModel<int>
    {
        public required string LoandNumber { get; set; }
        public required string FullNameCustomer { get; set; }
        public required decimal AprovechedCapital { get; set; }
        public required int QuantityInstallment { get; set; }
        public required int InstallmentPay { get; set; }
        public required decimal PendientAmount { get; set; }
        public required decimal AnnualInterestRate { get; set; }
        public required int Term { get; set; }
        public required bool StateLoans { get; set; }
        public required bool StateCustomer { get; set; }

    }
}
