using Artemis_Banking_Pro.Core.Application.DTOs.Base;
using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    public sealed class DetailLoansDto : BaseDto<int>
    {
        public required string NumberLoand { get; set; }
        public required string FullNameCustomer { get; set; }
        public required decimal ApprovedAmount { get; set; }
        public required decimal AnnualInterestRate { get; set; }
        public required int Term {  get; set; }
        public required LoanStatus StateLoans { get; set; }
        public required List<LoansInstallmentDto> loansInstallmentDtos { get; set; }
    }
}
