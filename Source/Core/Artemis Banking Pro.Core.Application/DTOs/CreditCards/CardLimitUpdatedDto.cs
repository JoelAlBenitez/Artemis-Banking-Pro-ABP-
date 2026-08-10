namespace Artemis_Banking_Pro.Core.Application.DTOs.CreditCards
{
    public sealed class CardLimitUpdatedDto
    {
        public required string CustomerId { get; set; }
        public required string LastFourDigits { get; set; }
        public required decimal CreditLimit { get; set; }
        public required DateTimeOffset ModifiedAt { get; set; }
    }
}
