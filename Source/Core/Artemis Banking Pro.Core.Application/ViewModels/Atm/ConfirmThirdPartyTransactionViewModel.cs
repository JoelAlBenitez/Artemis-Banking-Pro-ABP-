namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class ConfirmThirdPartyTransactionViewModel
    {
        public string SourceAccountNumber { get; set; }
        public string SourceAccountOwnerName { get; set; }
        public string DestinationAccountNumber { get; set; }
        public string DestinationAccountOwnerName { get; set; }
        public decimal Amount { get; set; }
    }
}
