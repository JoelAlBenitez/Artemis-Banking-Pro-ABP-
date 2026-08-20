using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts
{
    public sealed class SavingsAccountFilterDto
    {
        public string? IdCard { get; set; }
        public SavingsAccountStatusFilter Status { get; set; } = SavingsAccountStatusFilter.Activas;
        public SavingsAccountTypeFilter Type { get; set; } = SavingsAccountTypeFilter.Todas;
        public int Page { get; set; } = 1;

        //La Web App siempre pagina de 20 en 20; la Web API expone pageSize como parámetro y
        //el repositorio lo acota al máximo permitido.
        public int PageSize { get; set; } = DomainConstants.DefaultPageSize;
    }
}
