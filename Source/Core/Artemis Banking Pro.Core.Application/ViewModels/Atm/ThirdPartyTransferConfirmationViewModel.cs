namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class ThirdPartyTransferConfirmationViewModel : ThirdPartyTransferViewModel
    {
        public string OriginAccountHolderName { get; set; }
        public string DestinationAccountHolderName { get; set; }
    }
}
