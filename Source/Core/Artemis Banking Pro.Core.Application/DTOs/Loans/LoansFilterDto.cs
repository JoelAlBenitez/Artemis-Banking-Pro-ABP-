using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    public sealed class LoansFilterDto
    {
        public string? IdCard { get; set; }
        public LoanStatusFilter Status { get; set; } = LoanStatusFilter.Activos;
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = DomainConstants.DefaultPageSize;
    }
}
