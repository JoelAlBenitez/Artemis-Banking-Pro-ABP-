using Artemis_Banking_Pro.Core.Application.DTOs.Base;

namespace Artemis_Banking_Pro.Core.Application.DTOs.CreditCards
{
    public sealed class EditCardLimitDto : BaseDto<int>
    {
        public required decimal CreditLimit { get; set; }
    }
}
