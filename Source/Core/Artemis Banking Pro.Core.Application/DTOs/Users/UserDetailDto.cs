using Artemis_Banking_Pro.Core.Application.DTOs.Base;
using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace ArtemisBankingPro.Core.Application.DTOs.Users
{
    public class UserDetailDto : BaseDto<string>
    {
        public required string UserName { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public required string IDCARD { get; set; }
        public required string Email { get; set; }
        public Roles TypeUser { get; set; }
        public bool State { get; set; }
        public bool IsClient { get; set; }
    }
}
