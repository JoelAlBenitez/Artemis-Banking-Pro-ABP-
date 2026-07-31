namespace Artemis_Banking_Pro.Core.Application.DTOs.Transactions
{
    public sealed class BeneficiaryTransactionDto
    {
        public required string SourceAccountNumber { get; set; }
        public int BeneficiaryId { get; set; }
        public decimal Amount { get; set; }
    }
}
