using ArtemisBankingPro.Core.Application.DTOs.Account;

namespace ArtemisBankingPro.Core.Application.Contracts.Users.ExternalUsers
{
    public interface IAuthWebAppService
    {
        Task<AuthenticationResponse> LoginAsync(AuthenticationRequest request);
        Task LogoutAsync();
    }
}
