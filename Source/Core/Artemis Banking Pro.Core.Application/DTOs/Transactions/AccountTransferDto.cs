namespace Artemis_Banking_Pro.Core.Application.DTOs.Transactions
{
    public sealed class AccountTransferDto
    {
        public int SourceAccountId { get; set; }
        public int DestinationAccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
