using Artemis_Banking_Pro.Core.Application.DTOs.Base;
using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.CreditCards
{
    public sealed class CreditCardDto : BaseDto<int>
    {
        public required string MaskedCardNumber { get; set; }
        public required string CustomerId { get; set; }
        public required string FullNameCustomer { get; set; }
        public required decimal CreditLimit { get; set; }
        public required string ExpirationDate { get; set; }
        public required decimal OwedAmount { get; set; }
        public required decimal AvailableCredit { get; set; }
        public required CreditCardStatus StateCreditCard { get; set; }
    }
}
