namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class ConfirmLoanPaymentViewModel
    {
        public string SourceAccountNumber { get; set; }
        public string AccountOwnerName { get; set; }
        public string LoanOwnerName { get; set; }
        public string LoanNumber { get; set; }
        public decimal EnteredAmount { get; set; }
        public decimal EffectiveAmount { get; set; }
    }
}
