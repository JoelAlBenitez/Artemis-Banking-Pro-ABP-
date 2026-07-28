using Artemis_Banking_Pro.Core.Application.DTOs.Base;

namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    public sealed class EditAnnualInterestRate : BaseDto<int>
    {
        public required decimal AnnualInterestRate { get; set; }
    }
}
