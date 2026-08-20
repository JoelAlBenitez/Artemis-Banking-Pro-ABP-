namespace Artemis_Banking_Pro.Core.Application.ViewModels.Transactions
{
    public class ConfirmExpressViewModel
    {
        public string SourceAccountNumber { get; set; } = null!;
        public string OriginOwnerName { get; set; } = null!;
        public string DestinationAccountNumber { get; set; } = null!;
        public string DestinationOwnerName { get; set; } = null!;
        public decimal Amount { get; set; }
    }
}
