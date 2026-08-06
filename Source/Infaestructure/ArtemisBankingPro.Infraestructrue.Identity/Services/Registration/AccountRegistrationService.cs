using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.Contracts.Users.Password;
using ArtemisBankingPro.Core.Application.Contracts.Users.ExternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.InternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.Tokens;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArtemisBankingPro.Infraestructrue.Identity.Services.Registration
{
    public class AccountRegistrationService : IAccountRegistrationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountRegistrationService> _logger;

        public AccountRegistrationService(
            UserManager<ApplicationUser> userManager,
            ILogger<AccountRegistrationService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<RegisterResponse> RegisterUserAsync(RegisterRequest request)
        {
            _logger.LogInformation("Iniciando registro de usuario con email {Email}", request.Email);
            var response = new RegisterResponse();

            if (request.Password != request.ConfirmPassword)
            {
                _logger.LogWarning("Registro fallido: Las contraseÃ±as no coinciden para {Email}.", request.Email);
                response.HasError = true;
                response.Error = "La contraseÃ±a y la confirmaciÃ³n de contraseÃ±a deben coincidir.";
                return response;
            }

            var userWithSameEmail = await _userManager.FindByEmailAsync(request.Email);
            if (userWithSameEmail != null)
            {
                _logger.LogWarning("Registro fallido: El email {Email} ya estÃ¡ registrado.", request.Email);
                response.HasError = true;
                response.Error = "Ya existe un usuario registrado con este correo electrÃ³nico.";
                return response;
            }

            var userWithSameUserName = await _userManager.FindByNameAsync(request.UserName);
            if (userWithSameUserName != null)
            {
                _logger.LogWarning("Registro fallido: El nombre de usuario {UserName} ya estÃ¡ registrado.", request.UserName);
                response.HasError = true;
                response.Error = "Ya existe un usuario registrado con este nombre de usuario.";
                return response;
            }

            var userWithSameIdCard = await _userManager.Users.FirstOrDefaultAsync(u => u.IDCARD == request.IDCARD);
            if (userWithSameIdCard != null)
            {
                _logger.LogWarning("Registro fallido: La cÃ©dula {IdCard} ya estÃ¡ registrada.", request.IDCARD);
                response.HasError = true;
                response.Error = "Ya existe un usuario registrado con esta cÃ©dula.";
                return response;
            }

            var user = new ApplicationUser
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserName = request.UserName,
                IDCARD = request.IDCARD,
                IsActive = false
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                _logger.LogError("Error al intentar crear el usuario {UserName} en Identity.", request.UserName);
                response.HasError = true;
                response.Error = "Error al intentar crear el usuario. Por favor, verifique los datos ingresados.";
                return response;
            }

            await _userManager.AddToRoleAsync(user, request.Role);
            response.UserId = user.Id;

            _logger.LogInformation("Usuario {UserName} registrado exitosamente con rol {Role}.", request.UserName, request.Role);
            return response;
        }

        public async Task<string> ConfirmAccountAsync(string userId, string token)
        {
            _logger.LogInformation("Iniciando confirmaciÃ³n de cuenta para el usuario ID {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ConfirmaciÃ³n fallida: Usuario ID {UserId} no existe.", userId);
                return "El enlace de activaciÃ³n no es vÃ¡lido.";
            }

            if (user.EmailConfirmed)
            {
                _logger.LogWarning("ConfirmaciÃ³n fallida: El email de usuario {UserId} ya estaba confirmado.", userId);
                return "Este enlace de activaciÃ³n ya fue utilizado.";
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                _logger.LogWarning("ConfirmaciÃ³n fallida: Token invÃ¡lido o expirado para usuario {UserId}.", userId);
                return "El enlace de activaciÃ³n no es vÃ¡lido.";
            }

            user.IsActive = true;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Cuenta de usuario {UserId} confirmada y activada exitosamente.", userId);
            return "Su cuenta ha sido activada correctamente. Ya puede iniciar sesiÃ³n.";
        }
    }
}

