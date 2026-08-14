using ArtemisBankingPro.Core.Application.Contracts.Users.ExternalUsers;
using ArtemisBankingPro.Core.Application.DTOs.Account;
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

        //Roles con acceso a la aplicación web. Comercio queda fuera: solo usa la Web API.
        private static readonly string[] WebAppRoles =
        {
            nameof(Roles.Administrador),
            nameof(Roles.Cajero),
            nameof(Roles.Cliente)
        };

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
                _logger.LogWarning("Intento de login fallido: el usuario {UserName} no existe.", request.UserName);
                response.HasError = true;
                response.Error = "Los datos de acceso son inválidos.";
                return response;
            }

            //La contraseña se verifica antes que el estado y el rol: así una credencial
            //incorrecta nunca revela si la cuenta existe, está inactiva o qué rol tiene.
            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogWarning("Intento de login fallido: contraseña incorrecta para el usuario {UserName}.", request.UserName);
                response.HasError = true;
                response.Error = "Los datos de acceso son inválidos.";
                return response;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Intento de login denegado: la cuenta del usuario {UserName} está inactiva.", request.UserName);
                response.HasError = true;
                response.Error = "Su cuenta se encuentra inactiva. Debe activar su cuenta mediante el enlace enviado a su correo electrónico registrado para poder acceder al sistema.";
                return response;
            }

            var rolesList = await _userManager.GetRolesAsync(user);
            if (!rolesList.Any(role => WebAppRoles.Contains(role)))
            {
                _logger.LogWarning("Intento de login denegado: el usuario {UserName} no tiene un rol permitido en la aplicación web.", request.UserName);
                response.HasError = true;
                response.Error = "Este usuario no tiene permisos para acceder a la aplicación web.";
                return response;
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            response.Id = user.Id;
            response.Email = user.Email!;
            response.UserName = user.UserName!;
            response.Roles = rolesList.ToList();

            _logger.LogInformation("Login exitoso para el usuario {UserName} en la aplicación web.", request.UserName);
            return response;
        }

        public async Task LogoutAsync()
        {
            _logger.LogInformation("Cerrando la sesión del usuario en la aplicación web.");
            await _signInManager.SignOutAsync();
        }
    }
}
