using ArtemisBankingPro.Core.Application.DTOs.Account;

namespace ArtemisBankingPro.Core.Application.Contracts.Users.Tokens
{
    public interface IJwtTokenGenerator
    {
        Task<JwtResponseDto> GenerateJwtTokenAsync(string userId, string email, string userName, IList<string> roles);
    }
}
