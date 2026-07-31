using Artemis_Banking_Pro.Core.Application.DTOs.Base;
using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.CreditCards
{
    public sealed class CreditCardDto : BaseDto<int>
    {
        public required string CardNumber { get; set; }
        public required string ExpirationDate { get; set; }
        public required string ClientId { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal OwedAmount { get; set; }
        public required CreditCardStatus Status { get; set; }

        public string MaskedCardNumber => string.IsNullOrEmpty(CardNumber) || CardNumber.Length < 4 
            ? CardNumber 
            : $"**** **** **** {CardNumber[^4..]}";
    }
}
