using ArtemisBankingPro.Core.Domain.Entities.Base;
using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace ArtemisBankingPro.Core.Domain.Entities.Loans
{
    public sealed class Loan : BaseEntitie<int>
    {
        public required  string LoanNumber { get; set; }
        public required  string CustomerId { get; set; }
        public required  decimal ApprovedCapital { get; set; }
        public required TermMonths termMonths { get; set; }
        public required decimal AnnualInterestRate { get; set; }
        public decimal MonthlyInstallment { get; set; }
        public  decimal TotalPayable {  get; set; }
        public  decimal PendingAmount { get; set; }
        public required LoanStatus Status { get; set; }

        //Fecha en que todas las cuotas quedaron pagadas
        public DateTimeOffset? CompletedAt { get; set; }

        //Collections

        public ICollection<LoanInstallment> loanInstallments { get; set; } = new List<LoanInstallment>();
    }
}
