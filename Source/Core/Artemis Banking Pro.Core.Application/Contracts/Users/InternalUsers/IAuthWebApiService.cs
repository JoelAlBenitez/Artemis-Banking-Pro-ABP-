using ArtemisBankingPro.Core.Application.DTOs.Account;

namespace ArtemisBankingPro.Core.Application.Contracts.Users.InternalUsers
{
    public interface IAuthWebApiService
    {
        Task<LoginApiDtoResponse> LoginAsync(AuthenticationRequest request);
    }
}
