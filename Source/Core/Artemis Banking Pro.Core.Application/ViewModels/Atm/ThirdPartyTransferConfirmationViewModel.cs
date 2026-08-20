namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class ThirdPartyTransferConfirmationViewModel : ThirdPartyTransferViewModel
    {
        public required string OriginAccountHolderName { get; set; }
        public required string DestinationAccountHolderName { get; set; }
    }
}
