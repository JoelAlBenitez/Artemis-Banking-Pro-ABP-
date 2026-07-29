using Artemis_Banking_Pro.Core.Application.ViewModels.Base;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Loans
{
    public sealed class DetailsLoansViewModel : BaseViewModel<int>
    {
        public required string NumberLoand { get; set; }
        public required string FullNameCustomer { get; set; }
        public required decimal ApprovedAmount { get; set; }
        public required decimal AnnualInterestRate { get; set; }
        public required int Term { get; set; }
        public required bool StateLoans { get; set; }
        public required List<LoasInstallmentViewModel> loasInstallmentViewModels { get; set; }
    }
}
