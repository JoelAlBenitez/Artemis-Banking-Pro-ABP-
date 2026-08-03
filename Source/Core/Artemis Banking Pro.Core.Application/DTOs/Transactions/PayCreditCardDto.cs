namespace Artemis_Banking_Pro.Core.Application.DTOs.Transactions
{
    public sealed class PayCreditCardDto
    {
        public required string SourceAccountNumber { get; set; }
        public int CreditCardId { get; set; }
        public decimal Amount { get; set; }
    }
}
