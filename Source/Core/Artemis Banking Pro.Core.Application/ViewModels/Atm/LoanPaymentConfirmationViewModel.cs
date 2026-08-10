namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class LoanPaymentConfirmationViewModel : LoanPaymentViewModel
    {
        public string OriginAccountHolderName { get; set; }
        public string LoanHolderName { get; set; }
        public decimal EffectiveAmount { get; set; }
    }
}
