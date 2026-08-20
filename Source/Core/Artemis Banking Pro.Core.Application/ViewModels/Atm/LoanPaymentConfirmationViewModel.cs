namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class LoanPaymentConfirmationViewModel : LoanPaymentViewModel
    {
        public required string OriginAccountHolderName { get; set; }
        public required string LoanHolderName { get; set; }
        public decimal EffectiveAmount { get; set; }
    }
}
