using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace ArtemisBankingPro.Core.Domain.Entities.Loans
{
    public sealed class LoanInstallment 
    {
        public int Id { get; set; }
        public required int LoandId { get; set; }
        public required int InstallmentNumber { get; set; }
        public required DateTimeOffset DueDate { get; set; }
        public required decimal InstallmentValue { get; set; }
        public required decimal InterestAmount { get; set; }
        public required decimal CapitalAmount { get; set; }
        public required decimal PendingBalance { get; set; }
        public required PaymentStatus paymentStatus { get; set; }
        public bool IsOverdue { get; set; } 
        public DateTimeOffset? PaidAt { get; set; }
    }
}
