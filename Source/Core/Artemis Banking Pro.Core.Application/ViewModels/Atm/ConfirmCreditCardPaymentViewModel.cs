namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class ConfirmCreditCardPaymentViewModel
    {
        public string SourceAccountNumber { get; set; }
        public string AccountOwnerName { get; set; }
        public string CreditCardOwnerName { get; set; }
        public string CardLastFourDigits { get; set; }
        public decimal EnteredAmount { get; set; }
        public decimal EffectiveAmount { get; set; }
    }
}
