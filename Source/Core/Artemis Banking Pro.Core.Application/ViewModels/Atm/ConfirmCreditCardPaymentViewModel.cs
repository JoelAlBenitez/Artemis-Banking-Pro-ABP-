namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class ConfirmCreditCardPaymentViewModel
    {
        public required string SourceAccountNumber { get; set; }
        public required string AccountOwnerName { get; set; }
        public required string CreditCardNumber { get; set; }
        public required string CreditCardOwnerName { get; set; }
        public required string CardLastFourDigits { get; set; }
        public decimal EnteredAmount { get; set; }
        public decimal EffectiveAmount { get; set; }
    }
}
