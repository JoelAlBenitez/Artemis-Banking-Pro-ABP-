using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.Contracts.Users.Password;
using ArtemisBankingPro.Core.Application.Contracts.Users.ExternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.InternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.Tokens;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Web;

namespace ArtemisBankingPro.Infraestructrue.Identity.Services.Password
{
    public class PasswordRecoveryService : IPasswordRecoveryService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailServices _emailServices;
        private readonly ILogger<PasswordRecoveryService> _logger;

        private const string TokenProvider = "ResetPasswordProvider";
        private const string TokenDateKey = "TokenDate";
        private const string TokenUsedKey = "TokenUsed";

        public PasswordRecoveryService(
            UserManager<ApplicationUser> userManager,
            IEmailServices emailServices,
            ILogger<PasswordRecoveryService> logger)
        {
            _userManager = userManager;
            _emailServices = emailServices;
            _logger = logger;
        }

        public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, string origin)
        {
            _logger.LogInformation("Iniciando solicitud de restablecimiento de contraseÃ±a para el usuario {UserName}", request.UserName);
            var response = new ForgotPasswordResponse();
            var user = await _userManager.FindByNameAsync(request.UserName);

            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                _logger.LogWarning("Restablecimiento fallido: El usuario {UserName} no existe o no tiene correo registrado.", request.UserName);
                response.HasError = true;
                response.Error = "Este usuario no tiene un correo electrÃ³nico registrado. No es posible enviar la solicitud de restablecimiento.";
                return response;
            }

            // Invalida tokens anteriores cambiando el SecurityStamp
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                _logger.LogError("Error al actualizar SecurityStamp para el usuario {UserId}.", user.Id);
                response.HasError = true;
                response.Error = "No fue posible procesar la solicitud. Intente nuevamente.";
                return response;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            user.IsActive = false;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Error al desactivar la cuenta durante restablecimiento para {UserId}.", user.Id);
                response.HasError = true;
                response.Error = "No fue posible procesar la solicitud. Intente nuevamente.";
                return response;
            }

            // Guarda metadata del token (fecha de creaciÃ³n y estado de uso)
            var tokenDateResult = await _userManager.SetAuthenticationTokenAsync(user, TokenProvider, TokenDateKey, DateTime.UtcNow.ToString("o"));
            var tokenUsedResult = await _userManager.SetAuthenticationTokenAsync(user, TokenProvider, TokenUsedKey, "false");
            
            if (!tokenDateResult.Succeeded || !tokenUsedResult.Succeeded)
            {
                _logger.LogError("Error al guardar metadata del token para el usuario {UserId}.", user.Id);
                response.HasError = true;
                response.Error = "No fue posible procesar la solicitud. Intente nuevamente.";
                return response;
            }

            var encodedToken = HttpUtility.UrlEncode(token);
            var verificationUri = $"{origin}/Account/ResetPassword?token={encodedToken}&email={user.Email}";

            await _emailServices.SendNotification(new MessageDto
            {
                To = user.Email!,
                Subject = "Restablecimiento de contraseÃ±a",
                Message = $"<p>Hola {user.FirstName},</p><p>Hemos recibido una solicitud para restablecer la contraseÃ±a de su cuenta.</p><p>Para continuar, haga clic en el siguiente enlace:</p><p><a href='{verificationUri}'>{verificationUri}</a></p><p>Este enlace tendrÃ¡ una vigencia de 30 minutos.</p><p>Si usted no solicitÃ³ este cambio, ignore este mensaje.</p>"
            });

            _logger.LogInformation("Correo de restablecimiento enviado exitosamente a {Email} para {UserName}", user.Email, user.UserName);
            return response;
        }

        public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            _logger.LogInformation("Iniciando confirmaciÃ³n de nueva contraseÃ±a para la cuenta con email {Email}", request.Email);
            var response = new ResetPasswordResponse();

            if (request.Password != request.ConfirmPassword)
            {
                _logger.LogWarning("Fallo al restablecer contraseÃ±a: Las contraseÃ±as no coinciden para el email {Email}.", request.Email);
                response.HasError = true;
                response.Error = "La contraseÃ±a y la confirmaciÃ³n de contraseÃ±a deben coincidir.";
                return response;
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Fallo al restablecer contraseÃ±a: El usuario con email {Email} no existe.", request.Email);
                response.HasError = true;
                response.Error = "El enlace de restablecimiento no es vÃ¡lido.";
                return response;
            }

            // 1. Verificar si el token ya fue utilizado
            var tokenUsed = await _userManager.GetAuthenticationTokenAsync(user, TokenProvider, TokenUsedKey);
            if (tokenUsed == "true")
            {
                _logger.LogWarning("Fallo al restablecer contraseÃ±a: El token ya fue usado para {UserId}.", user.Id);
                response.HasError = true;
                response.Error = "Este enlace de restablecimiento ya fue utilizado.";
                return response;
            }

            // 2. Verificar si el token expirÃ³ (30 minutos)
            var tokenDateStr = await _userManager.GetAuthenticationTokenAsync(user, TokenProvider, TokenDateKey);
            if (!string.IsNullOrEmpty(tokenDateStr) &&
                DateTime.TryParse(tokenDateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var tokenDate))
            {
                if ((DateTime.UtcNow - tokenDate).TotalMinutes > 30)
                {
                    _logger.LogWarning("Fallo al restablecer contraseÃ±a: El token expirÃ³ para {UserId}.", user.Id);
                    response.HasError = true;
                    response.Error = "El enlace de restablecimiento ha expirado.";
                    return response;
                }
            }

            // 3. Ejecutar el restablecimiento en Identity
            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.Password);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Fallo al restablecer contraseÃ±a: Token invÃ¡lido o error de Identity para {UserId}.", user.Id);
                response.HasError = true;
                response.Error = "El enlace de restablecimiento no es vÃ¡lido.";
                return response;
            }

            // Marcar el token como usado e invalidar el SecurityStamp
            var setTokenResult = await _userManager.SetAuthenticationTokenAsync(user, TokenProvider, TokenUsedKey, "true");
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);

            user.IsActive = true;
            user.EmailConfirmed = true;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!setTokenResult.Succeeded || !stampResult.Succeeded || !updateResult.Succeeded)
            {
                _logger.LogError("Error crÃ­tico al guardar el restablecimiento (SetToken/SecurityStamp/Update) para {UserId}.", user.Id);
                response.HasError = true;
                response.Error = "OcurriÃ³ un error al finalizar el restablecimiento. Intente nuevamente.";
                return response;
            }

            _logger.LogInformation("ContraseÃ±a restablecida y cuenta reactivada exitosamente para {UserId}.", user.Id);

            return response;
        }

        // --- API METHODS ---

        public async Task<ForgotPasswordResponse> ForgotPasswordApiAsync(ForgotPasswordRequest request)
        {
            var response = new ForgotPasswordResponse();
            var user = await _userManager.FindByNameAsync(request.UserName);

            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                response.HasError = true;
                response.Error = "Este usuario no tiene un correo electrÃ³nico registrado. No es posible enviar la solicitud de restablecimiento.";
                return response;
            }

            // Invalida tokens anteriores cambiando el SecurityStamp
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                response.HasError = true;
                response.Error = "No fue posible procesar la solicitud. Intente nuevamente.";
                return response;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            // Guarda metadata del token (fecha de creaciÃ³n y estado de uso)
            await _userManager.SetAuthenticationTokenAsync(user, TokenProvider, TokenDateKey, DateTime.UtcNow.ToString("o"));
            await _userManager.SetAuthenticationTokenAsync(user, TokenProvider, TokenUsedKey, "false");

            await _emailServices.SendNotification(new MessageDto
            {
                To = user.Email!,
                Subject = "Token de restablecimiento de contraseÃ±a",
                Message = $"<p>Hola {user.FirstName},</p><p>Se ha generado un token para restablecer la contraseÃ±a de su cuenta.</p><p>Token de restablecimiento:<br><strong>{token}</strong></p><p>Utilice este token en el endpoint correspondiente para completar el cambio de contraseÃ±a.</p><p>Si usted no solicitÃ³ este cambio, ignore este mensaje.</p>"
            });

            return response;
        }

        public async Task<ResetPasswordResponse> ResetPasswordApiAsync(ResetPasswordApiRequest request)
        {
            var response = new ResetPasswordResponse();

            if (request.Password != request.ConfirmPassword)
            {
                response.HasError = true;
                response.Error = "La contraseÃ±a y la confirmaciÃ³n de contraseÃ±a deben coincidir.";
                return response;
            }

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                response.HasError = true;
                response.Error = "El enlace de restablecimiento no es vÃ¡lido.";
                return response;
            }

            // 1. Verificar si el token ya fue utilizado
            var tokenUsed = await _userManager.GetAuthenticationTokenAsync(user, TokenProvider, TokenUsedKey);
            if (tokenUsed == "true")
            {
                response.HasError = true;
                response.Error = "Este enlace de restablecimiento ya fue utilizado.";
                return response;
            }

            // 2. Verificar si el token expirÃ³ (30 minutos)
            var tokenDateStr = await _userManager.GetAuthenticationTokenAsync(user, TokenProvider, TokenDateKey);
            if (!string.IsNullOrEmpty(tokenDateStr) &&
                DateTime.TryParse(tokenDateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var tokenDate))
            {
                if ((DateTime.UtcNow - tokenDate).TotalMinutes > 30)
                {
                    response.HasError = true;
                    response.Error = "El enlace de restablecimiento ha expirado.";
                    return response;
                }
            }

            // 3. Ejecutar el restablecimiento en Identity
            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.Password);
            if (!result.Succeeded)
            {
                response.HasError = true;
                response.Error = "El enlace de restablecimiento no es vÃ¡lido.";
                return response;
            }

            // Marcar el token como usado e invalidar el SecurityStamp
            await _userManager.SetAuthenticationTokenAsync(user, TokenProvider, TokenUsedKey, "true");
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);

            user.IsActive = true;
            user.EmailConfirmed = true;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!stampResult.Succeeded || !updateResult.Succeeded)
            {
                response.HasError = true;
                response.Error = "OcurriÃ³ un error al finalizar el restablecimiento. Intente nuevamente.";
                return response;
            }

            return response;
        }
    }
}

