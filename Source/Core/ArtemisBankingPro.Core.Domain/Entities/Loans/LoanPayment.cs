using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Base;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;

namespace ArtemisBankingPro.Core.Domain.Entities.Loans
{
  
    public sealed class LoanPayment : BaseEntitie<int>
    {
        public required int LoandId { get; set; }
        public required int LoanInstallmentId {  get; set; }
        public required DateTimeOffset PaidAt { get; set; } = DateTimeOffset.UtcNow;
        public required decimal RequestedAmount { get; set; }
        public required decimal EffectiveAmount { get; set; }
        public required ChannelPayment Channel {  get; set; }
        public required string PerformedByUserId { get; set; }
        public int? TransactionId { get; set; }
        public  Loan? Loans { get; set; }
        public LoanInstallment? loanInstallment { get; set; }
        public Transaction? Transaction { get; set; }
    }
}
