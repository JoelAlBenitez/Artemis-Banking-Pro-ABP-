using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    public sealed class LoansInstallmentDto
    {
        public required int NumberLoanInstallment {  get; set; }
        public required DateTimeOffset DueDate { get; set; }
        public required decimal InstallmentValue { get; set; }

        //Desglose de la cuota que el detalle del préstamo expone en la Web API
        public decimal InterestAmount { get; set; }
        public decimal CapitalAmount { get; set; }

        public required decimal OutstandingBalance { get; set; }
        public required PaymentStatus StateInstallment { get; set; }
        public required bool IsOverdue { get; set; }

    }
}
