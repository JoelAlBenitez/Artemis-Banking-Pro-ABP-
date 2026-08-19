using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.Transactions
{
    public sealed class TransactionResultDto
    {
        public decimal EffectiveAmount { get; set; }
        public TransactionType TransactionType { get; set; }
        public TransactionStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? WarningMessage { get; set; }
        public string? Origin { get; set; }
        public string? Beneficiary { get; set; }
    }
}
