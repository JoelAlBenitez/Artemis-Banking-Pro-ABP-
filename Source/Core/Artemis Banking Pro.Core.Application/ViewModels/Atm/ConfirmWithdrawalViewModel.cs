namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class ConfirmWithdrawalViewModel
    {
        public string OriginAccountNumber { get; set; } = string.Empty;
        public string AccountOwnerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
