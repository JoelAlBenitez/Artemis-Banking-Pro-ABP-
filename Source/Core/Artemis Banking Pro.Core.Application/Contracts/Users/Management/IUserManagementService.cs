using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Contracts.Users.Management
{
    public interface IUserManagementService
    {
        // 1
        Task<PagedResponseDto<UserDto>> GetUsersAsync(int page, int pageSize, StatusFilter status);
        // 2
        Task<PagedResponseDto<UserDto>> GetUsersByRoleAsync(Roles role, int page, int pageSize);
        // 3
        Task<List<string>> GetRolesAsync();
        // 5
        Task<bool> ToggleUserAsync(string userId, string currentUserId);
        // 6
        Task<UserDetailDto?> GetUserByIdAsync(string userId);
        // 7
        Task<List<string>> GetRolesByUserAsync(string userId);
        // 8
        Task<ClientBaseDataDto?> GetClientBaseDataAsync(string userId);
        // 9
        Task<UserExistenceDto> ValidateUserExistsByIdAsync(string userId);
        Task<UserExistenceDto> ValidateUserExistsByIdCardAsync(string idCard);
        // 10
        Task<List<ClientSummaryDto>> GetActiveClientsAsync();
        // 11
        Task<List<string>> GetActiveClientIdsAsync();
        // 12
        Task<ClientSummaryDto?> GetClientByIdCardAsync(string idCard);
    }
}
