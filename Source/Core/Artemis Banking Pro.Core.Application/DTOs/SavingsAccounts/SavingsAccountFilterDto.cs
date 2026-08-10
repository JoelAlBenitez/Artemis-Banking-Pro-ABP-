using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts
{
    public sealed class SavingsAccountFilterDto
    {
        public string? IdCard { get; set; }
        public SavingsAccountStatusFilter Status { get; set; } = SavingsAccountStatusFilter.Activas;
        public SavingsAccountTypeFilter Type { get; set; } = SavingsAccountTypeFilter.Todas;
        public int Page { get; set; } = 1;
    }
}
