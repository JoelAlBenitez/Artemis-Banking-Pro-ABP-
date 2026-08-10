namespace Artemis_Banking_Pro.Core.Application.DTOs.Transactions
{
    public sealed class CashAdvanceRequestDto
    {
        public int CreditCardId { get; set; }
        public int SavingsAccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
