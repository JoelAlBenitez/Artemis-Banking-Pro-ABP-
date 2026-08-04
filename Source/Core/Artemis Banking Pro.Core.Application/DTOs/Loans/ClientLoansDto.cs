using Artemis_Banking_Pro.Core.Application.DTOs.Base;

namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    public sealed class ClientLoansDto: BaseDto<string>
    {
        public required string IdCard {  get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required decimal TotalDebtAmount { get; set; }
    }
}
