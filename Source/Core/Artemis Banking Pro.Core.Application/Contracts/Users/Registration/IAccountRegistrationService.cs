using ArtemisBankingPro.Core.Application.DTOs.Account;

namespace ArtemisBankingPro.Core.Application.Contracts.Users.Registration
{
    public interface IAccountRegistrationService
    {
        Task<RegisterResponse> RegisterUserAsync(RegisterRequest request);
        Task<string> ConfirmAccountAsync(string userId, string token);
    }
}
