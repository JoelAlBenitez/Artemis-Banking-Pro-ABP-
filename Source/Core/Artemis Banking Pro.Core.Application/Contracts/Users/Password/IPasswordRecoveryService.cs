using ArtemisBankingPro.Core.Application.DTOs.Account;

namespace ArtemisBankingPro.Core.Application.Contracts.Users.Password
{
    public interface IPasswordRecoveryService
    {
        Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, string origin);
        Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);

        // API Methods
        Task<ForgotPasswordResponse> ForgotPasswordApiAsync(ForgotPasswordRequest request);
        Task<ResetPasswordResponse> ResetPasswordApiAsync(ResetPasswordApiRequest request);
    }
}
