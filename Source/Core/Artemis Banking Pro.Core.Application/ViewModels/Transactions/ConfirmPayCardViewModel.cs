namespace Artemis_Banking_Pro.Core.Application.ViewModels.Transactions
{
    public class ConfirmPayCardViewModel
    {
        public string SourceAccountNumber { get; set; } = null!;
        public string OriginOwnerName { get; set; } = null!;
        public int CreditCardId { get; set; }
        public string CreditCardLastFour { get; set; } = null!;
        public string CreditCardOwnerName { get; set; } = null!;
        public decimal Amount { get; set; }
    }
}
