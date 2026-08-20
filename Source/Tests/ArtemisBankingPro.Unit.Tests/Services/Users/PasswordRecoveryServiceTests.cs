using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using ArtemisBankingPro.Infraestructrue.Identity.Interfaces;
using ArtemisBankingPro.Infraestructrue.Identity.Services.Password;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Users
{
    //Restablecimiento de contraseña: vigencia de 30 minutos, un solo uso y reactivación
    //de la cuenta al finalizar.
    public sealed class PasswordRecoveryServiceTests
    {
        private const string TokenProvider = "ResetPasswordProvider";
        private const string TokenDateKey = "TokenDate";
        private const string TokenUsedKey = "TokenUsed";

        private const string UserNotFound = "No existe un usuario registrado con este nombre de usuario.";
        private const string EmailNotRegistered =
            "Este usuario no tiene un correo electrónico registrado. No es posible enviar la solicitud de restablecimiento.";
        private const string RoleNotAllowed = "Este usuario no tiene permisos para acceder a la aplicación web.";
        private const string LinkExpired = "El enlace de restablecimiento ha expirado. Solicite un nuevo restablecimiento de contraseña.";
        private const string LinkAlreadyUsed = "Este enlace de restablecimiento ya fue utilizado.";
        private const string LinkNotValid = "El enlace de restablecimiento no es válido.";
        private const string PasswordsDoNotMatch = "La contraseña y la confirmación de contraseña deben coincidir.";

        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<IEmailServices> _emailServices;
        private readonly Mock<IGenerateTokens> _generateTokens;
        private readonly PasswordRecoveryService _service;

        public PasswordRecoveryServiceTests()
        {
            _userManager = IdentityMocks.UserManager();
            _emailServices = new Mock<IEmailServices>();
            _generateTokens = new Mock<IGenerateTokens>();

            _emailServices.Setup(e => e.SendNotification(It.IsAny<MessageDto>())).ReturnsAsync(true);
            _generateTokens
                .Setup(g => g.GenerateTokenResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync("token-de-prueba");

            _service = new PasswordRecoveryService(
                _userManager.Object,
                _emailServices.Object,
                NullLogger<PasswordRecoveryService>.Instance,
                _generateTokens.Object);
        }

        // ─── Solicitud del enlace ────────────────────────────────────────────

        [Fact]
        public async Task ForgotPasswordAsync_WithUnknownUserName_ShouldReportThatTheUserDoesNotExist()
        {
            _userManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            var response = await _service.ForgotPasswordAsync(Forgot("noexiste"), "https://localhost");

            response.HasError.Should().BeTrue();
            response.Error.Should().Be(UserNotFound);
            _emailServices.Verify(e => e.SendNotification(It.IsAny<MessageDto>()), Times.Never);
        }

        [Fact]
        public async Task ForgotPasswordAsync_WithoutARegisteredEmail_ShouldRejectTheRequest()
        {
            var user = IdentityMocks.BuildUser();
            user.Email = null;
            _userManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);

            var response = await _service.ForgotPasswordAsync(Forgot("usuario01"), "https://localhost");

            response.HasError.Should().BeTrue();
            response.Error.Should().Be(EmailNotRegistered);
        }

        [Fact]
        public async Task ForgotPasswordAsync_WithTheCommerceRole_ShouldRejectTheRequest()
        {
            GivenUserForForgot(IdentityMocks.BuildUser(), Roles.Comercio);

            var response = await _service.ForgotPasswordAsync(Forgot("comercio01"), "https://localhost");

            response.HasError.Should().BeTrue();
            response.Error.Should().Be(RoleNotAllowed);
        }

        //El documento exige inactivar la cuenta y registrar la fecha y el estado de uso del token
        [Fact]
        public async Task ForgotPasswordAsync_WithAValidUser_ShouldDeactivateTheAccountAndSendTheLink()
        {
            var user = IdentityMocks.BuildUser();
            GivenUserForForgot(user, Roles.Cliente);

            MessageDto? sent = null;
            _emailServices.Setup(e => e.SendNotification(It.IsAny<MessageDto>()))
                .Callback<MessageDto>(m => sent = m)
                .ReturnsAsync(true);

            var response = await _service.ForgotPasswordAsync(Forgot("usuario01"), "https://localhost");

            response.HasError.Should().BeFalse();
            user.IsActive.Should().BeFalse();

            sent.Should().NotBeNull();
            sent!.Subject.Should().Be("Restablecimiento de contraseña");
            sent.Message.Should().Contain("https://localhost/Account/ResetPassword?token=");
            sent.Message.Should().Contain("30 minutos");

            _userManager.Verify(m => m.SetAuthenticationTokenAsync(user, TokenProvider, TokenUsedKey, "false"), Times.Once);
            _userManager.Verify(m => m.SetAuthenticationTokenAsync(user, TokenProvider, TokenDateKey, It.IsAny<string>()), Times.Once);
        }

        //Desde la Web API el correo lleva el token en el cuerpo, nunca un enlace
        [Fact]
        public async Task ForgotPasswordApiAsync_ShouldSendTheTokenInTheBodyWithoutALink()
        {
            var user = IdentityMocks.BuildUser();
            GivenUserForForgot(user, Roles.Administrador);

            MessageDto? sent = null;
            _emailServices.Setup(e => e.SendNotification(It.IsAny<MessageDto>()))
                .Callback<MessageDto>(m => sent = m)
                .ReturnsAsync(true);

            var response = await _service.ForgotPasswordApiAsync(Forgot("adminapi"));

            response.HasError.Should().BeFalse();
            sent.Should().NotBeNull();
            sent!.Message.Should().Contain("token-de-prueba");
            sent.Message.Should().NotContain("/Account/ResetPassword");
        }

        [Theory]
        [InlineData(Roles.Cajero)]
        [InlineData(Roles.Cliente)]
        public async Task ForgotPasswordApiAsync_WithARoleThatDoesNotBelongToTheApi_ShouldRejectTheRequest(Roles role)
        {
            GivenUserForForgot(IdentityMocks.BuildUser(), role);

            var response = await _service.ForgotPasswordApiAsync(Forgot("usuario01"));

            response.HasError.Should().BeTrue();
            response.Error.Should().Be("Acceso denegado. No tiene permisos para utilizar este recurso.");
        }

        // ─── Creación de la nueva contraseña ─────────────────────────────────

        [Fact]
        public async Task ResetPasswordAsync_WithMismatchedPasswords_ShouldRejectTheChange()
        {
            var response = await _service.ResetPasswordAsync(Reset("Clave123*", "Otra456*"));

            response.HasError.Should().BeTrue();
            response.Error.Should().Be(PasswordsDoNotMatch);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithAnUnknownEmail_ShouldReportAnInvalidLink()
        {
            _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            var response = await _service.ResetPasswordAsync(Reset());

            response.HasError.Should().BeTrue();
            response.Error.Should().Be(LinkNotValid);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithAnAlreadyUsedToken_ShouldRejectTheChange()
        {
            var user = IdentityMocks.BuildUser();
            GivenUserForReset(user, tokenUsed: "true", tokenDate: DateTime.UtcNow);

            var response = await _service.ResetPasswordAsync(Reset());

            response.HasError.Should().BeTrue();
            response.Error.Should().Be(LinkAlreadyUsed);
            _userManager.Verify(m => m.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        //Vigencia máxima de 30 minutos
        [Fact]
        public async Task ResetPasswordAsync_WithATokenOlderThanThirtyMinutes_ShouldReportItAsExpired()
        {
            var user = IdentityMocks.BuildUser();
            GivenUserForReset(user, tokenUsed: "false", tokenDate: DateTime.UtcNow.AddMinutes(-31));

            var response = await _service.ResetPasswordAsync(Reset());

            response.HasError.Should().BeTrue();
            response.Error.Should().Be(LinkExpired);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithATokenOfTwentyNineMinutes_ShouldStillBeAccepted()
        {
            var user = IdentityMocks.BuildUser(isActive: false);
            GivenUserForReset(user, tokenUsed: "false", tokenDate: DateTime.UtcNow.AddMinutes(-29));

            var response = await _service.ResetPasswordAsync(Reset());

            response.HasError.Should().BeFalse();
        }

        //Al completar el proceso el token queda inválido y la cuenta vuelve a estar activa
        [Fact]
        public async Task ResetPasswordAsync_WithAValidToken_ShouldMarkItAsUsedAndReactivateTheAccount()
        {
            var user = IdentityMocks.BuildUser(isActive: false);
            GivenUserForReset(user, tokenUsed: "false", tokenDate: DateTime.UtcNow.AddMinutes(-5));

            var response = await _service.ResetPasswordAsync(Reset());

            response.HasError.Should().BeFalse();
            user.IsActive.Should().BeTrue();
            user.EmailConfirmed.Should().BeTrue();
            _userManager.Verify(m => m.SetAuthenticationTokenAsync(user, TokenProvider, TokenUsedKey, "true"), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordApiAsync_WithAValidToken_ShouldReactivateTheAccount()
        {
            var user = IdentityMocks.BuildUser(isActive: false);
            GivenUserForReset(user, tokenUsed: "false", tokenDate: DateTime.UtcNow.AddMinutes(-5));
            _userManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

            var response = await _service.ResetPasswordApiAsync(new ResetPasswordApiRequest
            {
                UserId = user.Id,
                Token = "token-de-prueba",
                Password = "Clave123*",
                ConfirmPassword = "Clave123*"
            });

            response.HasError.Should().BeFalse();
            user.IsActive.Should().BeTrue();
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static ForgotPasswordRequest Forgot(string userName) => new() { UserName = userName };

        private static ResetPasswordRequest Reset(string password = "Clave123*", string? confirm = null) => new()
        {
            Email = "usuario01@artemisbank.com",
            Token = "token-de-prueba",
            Password = password,
            ConfirmPassword = confirm ?? password
        };

        private void GivenUserForForgot(ApplicationUser user, params Roles[] roles)
        {
            _userManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(roles.Select(r => r.ToString()).ToList());
            _userManager.Setup(m => m.UpdateSecurityStampAsync(user)).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.SetAuthenticationTokenAsync(user, TokenProvider, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
        }

        private void GivenUserForReset(ApplicationUser user, string tokenUsed, DateTime tokenDate)
        {
            _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(m => m.GetAuthenticationTokenAsync(user, TokenProvider, TokenUsedKey)).ReturnsAsync(tokenUsed);
            _userManager.Setup(m => m.GetAuthenticationTokenAsync(user, TokenProvider, TokenDateKey))
                .ReturnsAsync(tokenDate.ToString("o"));
            _userManager.Setup(m => m.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.SetAuthenticationTokenAsync(user, TokenProvider, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.UpdateSecurityStampAsync(user)).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        }
    }
}
