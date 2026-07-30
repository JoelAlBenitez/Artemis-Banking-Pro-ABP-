using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    public sealed class LoansFilterDto
    {
        public LoanStatusFilter Status { get; set; } = LoanStatusFilter.Activos;
        public int Page { get; set; } = 1;
    }
}
