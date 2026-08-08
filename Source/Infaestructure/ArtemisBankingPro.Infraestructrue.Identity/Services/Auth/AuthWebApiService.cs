using ArtemisBankingPro.Core.Application.Contracts.Users.InternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.Tokens;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ArtemisBankingPro.Infraestructrue.Identity.Services.Auth
{
    public class AuthWebApiService : IAuthWebApiService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly ILogger<AuthWebApiService> _logger;

        //Roles con acceso a la Web API. Cajero y Cliente pertenecen solo a la aplicación web.
        private static readonly string[] WebApiRoles =
        {
            nameof(Roles.Administrador),
            nameof(Roles.Comercio)
        };

        public AuthWebApiService(
            UserManager<ApplicationUser> userManager,
            IJwtTokenGenerator jwtTokenGenerator,
            ILogger<AuthWebApiService> logger)
        {
            _userManager = userManager;
            _jwtTokenGenerator = jwtTokenGenerator;
            _logger = logger;
        }

        public async Task<LoginApiDtoResponse> LoginAsync(AuthenticationRequest request)
        {
            _logger.LogInformation("Iniciando proceso de login de la Web API para el usuario {UserName}", request.UserName);
            var response = new LoginApiDtoResponse
            {
                Token = null!,
                UserName = string.Empty,
                Roles = new List<string>()
            };

            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                _logger.LogWarning("Intento de login fallido: el usuario {UserName} no existe.", request.UserName);
                response.HasError = true;
                response.Error = "Los datos de acceso son inválidos.";
                return response;
            }

            //La contraseña se verifica primero: un 403 por rol solo debe emitirse ante
            //credenciales válidas.
            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogWarning("Intento de login fallido: contraseña incorrecta para el usuario {UserName}.", request.UserName);
                response.HasError = true;
                response.Error = "Los datos de acceso son inválidos.";
                return response;
            }

            var rolesList = await _userManager.GetRolesAsync(user);
            if (!rolesList.Any(role => WebApiRoles.Contains(role)))
            {
                _logger.LogWarning("Intento de login denegado: el usuario {UserName} no tiene un rol permitido en la Web API.", request.UserName);
                response.HasError = true;
                response.Forbidden = true;
                response.Error = "Acceso denegado. No tiene permisos para utilizar este recurso.";
                return response;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Intento de login denegado: la cuenta del usuario {UserName} está inactiva.", request.UserName);
                response.HasError = true;
                response.Error = "Su cuenta se encuentra inactiva. Debe activar su cuenta antes de iniciar sesión.";
                return response;
            }

            var jwt = await _jwtTokenGenerator.GenerateJwtTokenAsync(user.Id, user.Email!, user.UserName!, rolesList.ToList());

            response.Token = jwt.Token;
            response.UserName = user.UserName!;
            response.Roles = rolesList.ToList();
            response.Expiration = jwt.Expiration;

            _logger.LogInformation("Login exitoso para el usuario {UserName} en la Web API.", request.UserName);
            return response;
        }
    }
}
