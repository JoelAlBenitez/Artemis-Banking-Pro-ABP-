namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    public sealed class OverdueInstallmentsResultDto
    {
        public required int ReviewedInstallments { get; set; }

        public required int MarkedAsOverdue { get; set; }

        public required int OverdueMarkReverted { get; set; }

        public required int AffectedLoans { get; set; }
    }
}
