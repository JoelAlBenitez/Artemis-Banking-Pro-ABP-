using Artemis_Banking_Pro.Core.Application.DTOs.Base;
namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    public sealed class EditAnnualInterestRateDto :  BaseDto<int>
    {
        public required decimal AnnualInterestRate { get; set; }
    }
}
