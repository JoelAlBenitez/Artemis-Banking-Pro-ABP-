namespace Artemis_Banking_Pro.Core.Application.ViewModels.Transactions
{
    public class ConfirmAccountTransferViewModel
    {
        public int SourceAccountId { get; set; }
        public required string SourceAccountNumber { get; set; }
        public int DestinationAccountId { get; set; }
        public required string DestinationAccountNumber { get; set; }
        public decimal Amount { get; set; }
    }
}
