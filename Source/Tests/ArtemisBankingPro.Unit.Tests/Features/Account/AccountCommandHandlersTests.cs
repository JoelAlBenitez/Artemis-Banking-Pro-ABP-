using Artemis_Banking_Pro.Core.Application.Exceptions;
using Artemis_Banking_Pro.Core.Application.Features.Account.Commands.ConfirmAccount;
using Artemis_Banking_Pro.Core.Application.Features.Account.Commands.GetResetToken;
using Artemis_Banking_Pro.Core.Application.Features.Account.Commands.Login;
using Artemis_Banking_Pro.Core.Application.Features.Account.Commands.ResetPassword;
using ArtemisBankingPro.Core.Application.Contracts.Users.InternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.Password;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using FluentAssertions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Features.Account
{
    public sealed class AccountCommandHandlersTests
    {
        private const string InactiveAccount =
            "Su cuenta se encuentra inactiva. Debe activar su cuenta antes de iniciar sesión.";

        private const string AccessDenied =
            "Acceso denegado. No tiene permisos para utilizar este recurso.";

        private readonly Mock<IAuthWebApiService> _authService = new();
        private readonly Mock<IAccountRegistrationService> _registrationService = new();
        private readonly Mock<IPasswordRecoveryService> _passwordRecoveryService = new();

        #region Login

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnTheToken()
        {
            _authService
                .Setup(service => service.LoginAsync(It.IsAny<AuthenticationRequest>()))
                .ReturnsAsync(new LoginApiDtoResponse { Token = "jwt-emitido", UserName = "adminapi" });

            var handler = new LoginCommandHandler(_authService.Object);

            var result = await handler.Handle(
                new LoginCommand { UserName = "adminapi", Password = "AdminApi123*" },
                CancellationToken.None);

            result.Jwt.Should().Be("jwt-emitido");
        }

        //Credenciales incorrectas y cuenta inactiva comparten respuesta: 401 Unauthorized.
        [Fact]
        public async Task Login_WithInactiveAccount_ShouldRejectAsUnauthorized()
        {
            _authService
                .Setup(service => service.LoginAsync(It.IsAny<AuthenticationRequest>()))
                .ReturnsAsync(new LoginApiDtoResponse
                {
                    Token = null!,
                    UserName = string.Empty,
                    HasError = true,
                    Error = InactiveAccount
                });

            var handler = new LoginCommandHandler(_authService.Object);

            var act = async () => await handler.Handle(
                new LoginCommand { UserName = "cliente01", Password = "Cliente123*" },
                CancellationToken.None);

            var exception = await act.Should().ThrowAsync<UnauthorizedException>();
            exception.WithMessage(InactiveAccount);
        }

        //Un rol ajeno a la API sí distingue: no es un fallo de autenticación sino de permisos.
        [Fact]
        public async Task Login_WithRoleNotAllowedInTheApi_ShouldRejectAsForbidden()
        {
            _authService
                .Setup(service => service.LoginAsync(It.IsAny<AuthenticationRequest>()))
                .ReturnsAsync(new LoginApiDtoResponse
                {
                    Token = null!,
                    UserName = string.Empty,
                    HasError = true,
                    Forbidden = true,
                    Error = AccessDenied
                });

            var handler = new LoginCommandHandler(_authService.Object);

            var act = async () => await handler.Handle(
                new LoginCommand { UserName = "cajerouser", Password = "Cajero123*" },
                CancellationToken.None);

            var exception = await act.Should().ThrowAsync<ForbiddenException>();
            exception.WithMessage(AccessDenied);
        }

        #endregion

        #region Confirmar cuenta

        [Fact]
        public async Task ConfirmAccount_WithValidToken_ShouldActivateTheAccount()
        {
            _registrationService
                .Setup(service => service.ConfirmAccountAsync("2", "token-valido"))
                .ReturnsAsync(new ConfirmAccountResponse { Message = "Cuenta confirmada." });

            var handler = new ConfirmAccountCommandHandler(_registrationService.Object);

            var act = async () => await handler.Handle(
                new ConfirmAccountCommand { UserId = "2", Token = "token-valido" },
                CancellationToken.None);

            await act.Should().NotThrowAsync();
            _registrationService.Verify(service => service.ConfirmAccountAsync("2", "token-valido"), Times.Once);
        }

        [Fact]
        public async Task ConfirmAccount_WithUsedToken_ShouldRejectTheRequest()
        {
            _registrationService
                .Setup(service => service.ConfirmAccountAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ConfirmAccountResponse
                {
                    HasError = true,
                    Message = "El token ya fue utilizado."
                });

            var handler = new ConfirmAccountCommandHandler(_registrationService.Object);

            var act = async () => await handler.Handle(
                new ConfirmAccountCommand { UserId = "2", Token = "token-usado" },
                CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("El token ya fue utilizado.");
        }

        #endregion

        #region Token de restablecimiento

        //La variante de API inactiva la cuenta y envía el token en el cuerpo del correo: el
        //handler debe llamar a ForgotPasswordApiAsync, nunca a la de la aplicación web.
        [Fact]
        public async Task GetResetToken_ShouldUseTheApiVariantOfTheService()
        {
            _passwordRecoveryService
                .Setup(service => service.ForgotPasswordApiAsync(It.IsAny<ForgotPasswordRequest>()))
                .ReturnsAsync(new ForgotPasswordResponse());

            var handler = new GetResetTokenCommandHandler(_passwordRecoveryService.Object);

            await handler.Handle(new GetResetTokenCommand { UserName = "adminapi" }, CancellationToken.None);

            _passwordRecoveryService.Verify(
                service => service.ForgotPasswordApiAsync(
                    It.Is<ForgotPasswordRequest>(request => request.UserName == "adminapi")),
                Times.Once);

            _passwordRecoveryService.Verify(
                service => service.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequest>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GetResetToken_WithUnknownUser_ShouldRejectTheRequest()
        {
            _passwordRecoveryService
                .Setup(service => service.ForgotPasswordApiAsync(It.IsAny<ForgotPasswordRequest>()))
                .ReturnsAsync(new ForgotPasswordResponse
                {
                    HasError = true,
                    Error = "El usuario indicado no existe."
                });

            var handler = new GetResetTokenCommandHandler(_passwordRecoveryService.Object);

            var act = async () => await handler.Handle(
                new GetResetTokenCommand { UserName = "inexistente" }, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("El usuario indicado no existe.");
        }

        #endregion

        #region Reseteo de contraseña

        [Fact]
        public async Task ResetPassword_ShouldForwardTheTokenAndTheNewPassword()
        {
            _passwordRecoveryService
                .Setup(service => service.ResetPasswordApiAsync(It.IsAny<ResetPasswordApiRequest>()))
                .ReturnsAsync(new ResetPasswordResponse());

            var handler = new ResetPasswordCommandHandler(_passwordRecoveryService.Object);

            await handler.Handle(new ResetPasswordCommand
            {
                UserId = "1",
                Token = "token-valido",
                Password = "123P@$$word!",
                ConfirmPassword = "123P@$$word!"
            }, CancellationToken.None);

            _passwordRecoveryService.Verify(
                service => service.ResetPasswordApiAsync(It.Is<ResetPasswordApiRequest>(request =>
                    request.UserId == "1" &&
                    request.Token == "token-valido" &&
                    request.Password == "123P@$$word!")),
                Times.Once);
        }

        [Fact]
        public async Task ResetPassword_WithInvalidToken_ShouldRejectTheRequest()
        {
            _passwordRecoveryService
                .Setup(service => service.ResetPasswordApiAsync(It.IsAny<ResetPasswordApiRequest>()))
                .ReturnsAsync(new ResetPasswordResponse
                {
                    HasError = true,
                    Error = "El token no es válido."
                });

            var handler = new ResetPasswordCommandHandler(_passwordRecoveryService.Object);

            var act = async () => await handler.Handle(new ResetPasswordCommand
            {
                UserId = "1",
                Token = "token-invalido",
                Password = "123P@$$word!",
                ConfirmPassword = "123P@$$word!"
            }, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("El token no es válido.");
        }

        #endregion
    }
}
