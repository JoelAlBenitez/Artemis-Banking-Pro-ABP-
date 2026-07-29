using Artemis_Banking_Pro.Core.Application.DTOs.Base;
using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    public sealed class LoansDto : BaseDto<int>
    {
        public required string LoanNumber { get; set; }
        public required string FullNameCustomer { get; set; }
        public required decimal AprovechedCapital { get; set; }
        public required int QuantityInstallment { get; set; }
        public required int InstallmentPay {  get; set; }
        public required decimal PendientAmount { get; set; }
        public required decimal AnnualInterestRate { get; set; }
        public required int Term {  get; set; }
        public required LoanStatus StateLoans {  get; set; }

        //En mora si tiene al menos una cuota atrasada
        public required bool CustomerInArrears { get; set; }

    }
}
