namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class ConfirmDepositViewModel
    {
        public string DestinationAccountNumber { get; set; } = string.Empty;
        public string AccountOwnerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
