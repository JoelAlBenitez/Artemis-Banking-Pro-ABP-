using ArtemisBankingPro.Core.Application.DTOs.Account;

namespace ArtemisBankingPro.Core.Application.Contracts.Users.Registration
{
    public interface IAccountRegistrationService
    {
        //Crea el usuario inactivo, su cuenta de ahorro principal cuando corresponde y envía
        //el correo de activación. Origin distingue la aplicación web (enlace) de la Web API (token).
        Task<RegisterResponse> RegisterUserAsync(RegisterRequest request);
        Task<ConfirmAccountResponse> ConfirmAccountAsync(string userId, string token);
    }
}
