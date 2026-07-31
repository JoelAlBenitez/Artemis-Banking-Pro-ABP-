using Artemis_Banking_Pro.Core.Application.DTOs.Base;
using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts
{
    public sealed class SavingsAccountDto : BaseDto<int>
    {
        public required string AccountNumber { get; set; }
        public required string ClientId { get; set; }
        public decimal Balance { get; set; }
        public required SavingsAccountType Type { get; set; }
        public required SavingsAccountStatus Status { get; set; }
    }
}
