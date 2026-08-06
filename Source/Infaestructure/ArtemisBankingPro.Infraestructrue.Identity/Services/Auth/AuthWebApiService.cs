using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.Contracts.Users.Password;
using ArtemisBankingPro.Core.Application.Contracts.Users.ExternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.InternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.Tokens;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

namespace ArtemisBankingPro.Infraestructrue.Identity.Services.Auth
{
    public class AuthWebApiService : IAuthWebApiService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly ILogger<AuthWebApiService> _logger;

        public AuthWebApiService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenGenerator jwtTokenGenerator,
            ILogger<AuthWebApiService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenGenerator = jwtTokenGenerator;
            _logger = logger;
        }

        public async Task<LoginApiDtoResponse> LoginAsync(AuthenticationRequest request)
        {
            _logger.LogInformation("Iniciando proceso de login WebApi para el usuario {UserName}", request.UserName);
            var response = new LoginApiDtoResponse
            {
                Token = null!,
                UserName = string.Empty,
                Roles = new List<string>()
            };

            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                _logger.LogWarning("Intento de login fallido: El usuario {UserName} no existe.", request.UserName);
                response.HasError = true;
                response.Error = "Los datos de acceso son invÃ¡lidos.";
                return response;
            }

            var rolesList = await _userManager.GetRolesAsync(user);
            if (rolesList.Contains(Roles.Cajero.ToString()) || rolesList.Contains(Roles.Cliente.ToString()))
            {
                _logger.LogWarning("Intento de login denegado: El usuario {UserName} no tiene permisos para usar WebApi.", request.UserName);
                response.HasError = true;
                response.Forbidden = true;
                response.Error = "Acceso denegado. No tiene permisos para utilizar este recurso.";
                return response;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Intento de login denegado: La cuenta del usuario {UserName} estÃ¡ inactiva.", request.UserName);
                response.HasError = true;
                response.Error = "Su cuenta se encuentra inactiva. Debe activar su cuenta antes de iniciar sesiÃ³n.";
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

            var jwt = await _jwtTokenGenerator.GenerateJwtTokenAsync(user.Id, user.Email!, user.UserName!, rolesList.ToList());

            response.Token = jwt.Token;
            response.UserName = user.UserName!;
            response.Roles = rolesList.ToList();
            response.Expiration = jwt.Expiration;

            _logger.LogInformation("Login exitoso para el usuario {UserName} en WebApi.", request.UserName);
            return response;
        }
    }
}

