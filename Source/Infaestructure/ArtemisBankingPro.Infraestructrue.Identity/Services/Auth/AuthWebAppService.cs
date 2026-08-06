using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.Contracts.Users.Password;
using ArtemisBankingPro.Core.Application.Contracts.Users.ExternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.InternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.Tokens;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ArtemisBankingPro.Infraestructrue.Identity.Services.Auth
{
    public class AuthWebAppService : IAuthWebAppService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthWebAppService> _logger;

        public AuthWebAppService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthWebAppService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<AuthenticationResponse> LoginAsync(AuthenticationRequest request)
        {
            _logger.LogInformation("Iniciando proceso de login WebApp para el usuario {UserName}", request.UserName);
            var response = new AuthenticationResponse();

            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                _logger.LogWarning("Intento de login fallido: El usuario {UserName} no existe.", request.UserName);
                response.HasError = true;
                response.Error = "Los datos de acceso son invÃ¡lidos.";
                return response;
            }

            var rolesList = await _userManager.GetRolesAsync(user);
            if (rolesList.Contains(Roles.Comercio.ToString()))
            {
                _logger.LogWarning("Intento de login denegado: El usuario {UserName} es un comercio y no tiene acceso a WebApp.", request.UserName);
                response.HasError = true;
                response.Error = "Este usuario no tiene permisos para acceder a la aplicaciÃ³n web.";
                return response;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Intento de login denegado: La cuenta del usuario {UserName} estÃ¡ inactiva.", request.UserName);
                response.HasError = true;
                response.Error = "Su cuenta se encuentra inactiva. Debe activar su cuenta mediante el enlace enviado a su correo electrÃ³nico registrado para poder acceder al sistema.";
                return response;
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, request.Password, false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Intento de login fallido: ContraseÃ±a incorrecta para el usuario {UserName}.", request.UserName);
                response.HasError = true;
                response.Error = "Los datos de acceso son invÃ¡lidos.";
                return response;
            }

            response.Id = user.Id;
            response.Email = user.Email!;
            response.UserName = user.UserName!;
            response.Roles = rolesList.ToList();

            _logger.LogInformation("Login exitoso para el usuario {UserName} en WebApp.", request.UserName);
            return response;
        }

        public async Task LogoutAsync()
        {
            _logger.LogInformation("Cerrando sesiÃ³n de usuario en WebApp.");
            await _signInManager.SignOutAsync();
        }
    }
}

