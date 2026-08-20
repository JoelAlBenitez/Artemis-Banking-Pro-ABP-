namespace Artemis_Banking_Pro.Core.Application.ViewModels.Transactions
{
    public class ConfirmCashAdvanceViewModel
    {
        public string SourceCardNumber { get; set; } = null!;
        public string DestinationAccountNumber { get; set; } = null!;
        public decimal Amount { get; set; }
        
        // Added properties to map back to original submission
        public int CreditCardId { get; set; }
        public int SavingsAccountId { get; set; }
    }
}
