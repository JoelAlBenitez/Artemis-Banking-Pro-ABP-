using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using ArtemisBankingPro.Core.Application.Contracts.Users.Password;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using ArtemisBankingPro.Infraestructrue.Identity.Interfaces;
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
        private readonly IGenerateTokens _generateTokens;

        private const string TokenProvider = "ResetPasswordProvider";
        private const string TokenDateKey = "TokenDate";
        private const string TokenUsedKey = "TokenUsed";

        //Vigencia máxima del enlace de restablecimiento exigida por el documento funcional
        private const int TokenLifetimeInMinutes = 30;

        private static readonly string[] WebAppRoles =
        {
            nameof(Roles.Administrador),
            nameof(Roles.Cajero),
            nameof(Roles.Cliente)
        };

        private static readonly string[] WebApiRoles =
        {
            nameof(Roles.Administrador),
            nameof(Roles.Comercio)
        };

        public PasswordRecoveryService(
            UserManager<ApplicationUser> userManager,
            IEmailServices emailServices,
            ILogger<PasswordRecoveryService> logger,
            IGenerateTokens generateTokens)
        {
            _userManager = userManager;
            _emailServices = emailServices;
            _logger = logger;
            _generateTokens = generateTokens;
        }

        public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, string origin)
        {
            _logger.LogInformation("Iniciando la solicitud de restablecimiento de contraseña del usuario {UserName}", request.UserName);
            var response = new ForgotPasswordResponse();

            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                _logger.LogWarning("Restablecimiento fallido: el usuario {UserName} no existe.", request.UserName);
                response.HasError = true;
                response.Error = "No existe un usuario registrado con este nombre de usuario.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning("Restablecimiento fallido: el usuario {UserName} no tiene correo registrado.", request.UserName);
                response.HasError = true;
                response.Error = "Este usuario no tiene un correo electrónico registrado. No es posible enviar la solicitud de restablecimiento.";
                return response;
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any(role => WebAppRoles.Contains(role)))
            {
                _logger.LogWarning("Restablecimiento denegado: el usuario {UserName} no tiene un rol permitido en la aplicación web.", request.UserName);
                response.HasError = true;
                response.Error = "Este usuario no tiene permisos para acceder a la aplicación web.";
                return response;
            }

            var token = await PrepareResetTokenAsync(user);
            if (token == null)
            {
                response.HasError = true;
                response.Error = "No fue posible procesar la solicitud. Intente nuevamente.";
                return response;
            }

            var encodedToken = HttpUtility.UrlEncode(token);
            var verificationUri = $"{origin}/Account/ResetPassword?token={encodedToken}&email={user.Email}";

            await _emailServices.SendNotification(new MessageDto
            {
                To = user.Email!,
                Subject = "Restablecimiento de contraseña",
                Message = $"<p>Hola {user.FirstName},</p>" +
                          "<p>Hemos recibido una solicitud para restablecer la contraseña de su cuenta.</p>" +
                          "<p>Para continuar, haga clic en el siguiente enlace:</p>" +
                          $"<p><a href='{verificationUri}'>{verificationUri}</a></p>" +
                          $"<p>Este enlace tendrá una vigencia de {TokenLifetimeInMinutes} minutos.</p>" +
                          "<p>Si usted no solicitó este cambio, ignore este mensaje.</p>"
            });

            _logger.LogInformation("Correo de restablecimiento enviado a {Email} para el usuario {UserName}", user.Email, user.UserName);
            return response;
        }

        public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            _logger.LogInformation("Iniciando el cambio de contraseña de la cuenta con correo {Email}", request.Email);

            var user = await _userManager.FindByEmailAsync(request.Email);
            return await CompleteResetAsync(user, request.Token, request.Password, request.ConfirmPassword);
        }

        // --- Web API ---

        public async Task<ForgotPasswordResponse> ForgotPasswordApiAsync(ForgotPasswordRequest request)
        {
            var response = new ForgotPasswordResponse();

            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                response.HasError = true;
                response.Error = "No existe un usuario registrado con este nombre de usuario.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                response.HasError = true;
                response.Error = "Este usuario no tiene un correo electrónico registrado. No es posible enviar la solicitud de restablecimiento.";
                return response;
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any(role => WebApiRoles.Contains(role)))
            {
                response.HasError = true;
                response.Error = "Acceso denegado. No tiene permisos para utilizar este recurso.";
                return response;
            }

            var token = await PrepareResetTokenAsync(user);
            if (token == null)
            {
                response.HasError = true;
                response.Error = "No fue posible procesar la solicitud. Intente nuevamente.";
                return response;
            }

            //Desde la API el correo no lleva enlace: el token viaja en el cuerpo para
            //utilizarse en el endpoint de reseteo.
            await _emailServices.SendNotification(new MessageDto
            {
                To = user.Email!,
                Subject = "Token de restablecimiento de contraseña",
                Message = $"<p>Hola {user.FirstName},</p>" +
                          "<p>Se ha generado un token para restablecer la contraseña de su cuenta.</p>" +
                          $"<p>Token de restablecimiento:<br><strong>{token}</strong></p>" +
                          $"<p>Identificador de usuario: <strong>{user.Id}</strong></p>" +
                          "<p>Utilice este token en el endpoint correspondiente para completar el cambio de contraseña.</p>" +
                          "<p>Si usted no solicitó este cambio, ignore este mensaje.</p>"
            });

            return response;
        }

        public async Task<ResetPasswordResponse> ResetPasswordApiAsync(ResetPasswordApiRequest request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            return await CompleteResetAsync(user, request.Token, request.Password, request.ConfirmPassword);
        }

        #region Helpers

        //Inactiva la cuenta, invalida los tokens anteriores y registra la fecha de emisión
        //y el estado de uso del nuevo token. Devuelve null si algo falló.
        private async Task<string?> PrepareResetTokenAsync(ApplicationUser user)
        {
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                _logger.LogError("Error al actualizar el SecurityStamp del usuario {UserId}.", user.Id);
                return null;
            }

            var token = await _generateTokens.GenerateTokenResetPasswordAsync(user, string.Empty);

            user.IsActive = false;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Error al inactivar temporalmente la cuenta del usuario {UserId}.", user.Id);
                return null;
            }

            var tokenDateResult = await _userManager.SetAuthenticationTokenAsync(user, TokenProvider, TokenDateKey, DateTime.UtcNow.ToString("o"));
            var tokenUsedResult = await _userManager.SetAuthenticationTokenAsync(user, TokenProvider, TokenUsedKey, "false");

            if (!tokenDateResult.Succeeded || !tokenUsedResult.Succeeded)
            {
                _logger.LogError("Error al guardar los datos del token del usuario {UserId}.", user.Id);
                return null;
            }

            return token;
        }

        //Validaciones y efectos comunes a la aplicación web y a la Web API: token existente,
        //no expirado, no utilizado, contraseñas coincidentes y reactivación de la cuenta.
        private async Task<ResetPasswordResponse> CompleteResetAsync(
            ApplicationUser? user, string token, string password, string confirmPassword)
        {
            var response = new ResetPasswordResponse();

            if (password != confirmPassword)
            {
                response.HasError = true;
                response.Error = "La contraseña y la confirmación de contraseña deben coincidir.";
                return response;
            }

            if (user == null)
            {
                response.HasError = true;
                response.Error = "El enlace de restablecimiento no es válido.";
                return response;
            }

            var tokenUsed = await _userManager.GetAuthenticationTokenAsync(user, TokenProvider, TokenUsedKey);
            if (tokenUsed == "true")
            {
                _logger.LogWarning("Restablecimiento fallido: el token ya fue utilizado por el usuario {UserId}.", user.Id);
                response.HasError = true;
                response.Error = "Este enlace de restablecimiento ya fue utilizado.";
                return response;
            }

            var tokenDateStr = await _userManager.GetAuthenticationTokenAsync(user, TokenProvider, TokenDateKey);
            if (!string.IsNullOrEmpty(tokenDateStr) &&
                DateTime.TryParse(tokenDateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var tokenDate) &&
                (DateTime.UtcNow - tokenDate).TotalMinutes > TokenLifetimeInMinutes)
            {
                _logger.LogWarning("Restablecimiento fallido: el token del usuario {UserId} expiró.", user.Id);
                response.HasError = true;
                response.Error = "El enlace de restablecimiento ha expirado. Solicite un nuevo restablecimiento de contraseña.";
                return response;
            }

            var result = await _userManager.ResetPasswordAsync(user, token, password);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Restablecimiento fallido: token inválido para el usuario {UserId}.", user.Id);
                response.HasError = true;
                response.Error = "El enlace de restablecimiento no es válido.";
                return response;
            }

            //El token queda inválido tras el cambio y la cuenta vuelve a estar activa
            var setTokenResult = await _userManager.SetAuthenticationTokenAsync(user, TokenProvider, TokenUsedKey, "true");
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);

            user.IsActive = true;
            user.EmailConfirmed = true;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!setTokenResult.Succeeded || !stampResult.Succeeded || !updateResult.Succeeded)
            {
                _logger.LogError("Error al finalizar el restablecimiento del usuario {UserId}.", user.Id);
                response.HasError = true;
                response.Error = "Ocurrió un error al finalizar el restablecimiento. Intente nuevamente.";
                return response;
            }

            _logger.LogInformation("Contraseña restablecida y cuenta reactivada para el usuario {UserId}.", user.Id);
            return response;
        }

        #endregion
    }
}
