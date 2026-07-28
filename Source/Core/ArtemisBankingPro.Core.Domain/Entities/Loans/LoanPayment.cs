using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace ArtemisBankingPro.Core.Domain.Entities.Loans
{
    public sealed class LoanPayment
    {
        public int Id { get; set; }
        public required int LoandId { get; set; }
        public required int LoanInstallment {  get; set; }
        public required DateTimeOffset PaidAt { get; set; } = DateTimeOffset.UtcNow;
        public required decimal EffectiveAmount { get; set; }
        public required ChannelPayment Channel {  get; set; }
        public required string PerformedByUserId { get; set; }
        public  Loan? Loans { get; set; }
        public LoanInstallment? loanInstallment { get; set; }
        
    }
}
