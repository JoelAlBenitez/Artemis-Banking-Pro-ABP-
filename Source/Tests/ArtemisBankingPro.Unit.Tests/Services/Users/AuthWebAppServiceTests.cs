using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using ArtemisBankingPro.Infraestructrue.Identity.Services.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Users
{
    //Login de la aplicación web. Los mensajes son literales del documento funcional: si se
    //editan aquí sin editarlos allá, la prueba deja de proteger nada.
    public sealed class AuthWebAppServiceTests
    {
        private const string InvalidCredentials = "Los datos de acceso son inválidos.";
        private const string InactiveAccount =
            "Su cuenta se encuentra inactiva. Debe activar su cuenta mediante el enlace enviado a su correo electrónico registrado para poder acceder al sistema.";
        private const string RoleNotAllowed = "Este usuario no tiene permisos para acceder a la aplicación web.";

        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManager;
        private readonly AuthWebAppService _service;

        public AuthWebAppServiceTests()
        {
            _userManager = IdentityMocks.UserManager();
            _signInManager = IdentityMocks.SignInManager(_userManager);

            _service = new AuthWebAppService(
                _userManager.Object,
                _signInManager.Object,
                NullLogger<AuthWebAppService>.Instance);
        }

        [Fact]
        public async Task LoginAsync_WithUnknownUserName_ShouldReturnTheInvalidCredentialsMessage()
        {
            _userManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            var response = await _service.LoginAsync(Request("noexiste", "clave"));

            response.HasError.Should().BeTrue();
            response.Error.Should().Be(InvalidCredentials);
        }

        [Fact]
        public async Task LoginAsync_WithWrongPassword_ShouldReturnTheInvalidCredentialsMessage()
        {
            GivenUser(IdentityMocks.BuildUser(), passwordIsValid: false, Roles.Cliente);

            var response = await _service.LoginAsync(Request("usuario01", "incorrecta"));

            response.HasError.Should().BeTrue();
            response.Error.Should().Be(InvalidCredentials);
        }

        //La contraseña se valida antes que el estado: una credencial incorrecta nunca puede
        //revelar que la cuenta existe pero está inactiva.
        [Fact]
        public async Task LoginAsync_WithWrongPasswordOnAnInactiveAccount_ShouldNotRevealThatItIsInactive()
        {
            GivenUser(IdentityMocks.BuildUser(isActive: false), passwordIsValid: false, Roles.Cliente);

            var response = await _service.LoginAsync(Request("usuario01", "incorrecta"));

            response.Error.Should().Be(InvalidCredentials);
        }

        [Fact]
        public async Task LoginAsync_WithAnInactiveAccount_ShouldAskTheUserToActivateIt()
        {
            GivenUser(IdentityMocks.BuildUser(isActive: false), passwordIsValid: true, Roles.Cliente);

            var response = await _service.LoginAsync(Request("usuario01", "correcta"));

            response.HasError.Should().BeTrue();
            response.Error.Should().Be(InactiveAccount);
            _signInManager.Verify(m => m.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), null), Times.Never);
        }

        //El rol Comercio pertenece únicamente a la Web API y al procesador Hermes Pay
        [Fact]
        public async Task LoginAsync_WithTheCommerceRole_ShouldDenyAccessToTheWebApplication()
        {
            GivenUser(IdentityMocks.BuildUser(), passwordIsValid: true, Roles.Comercio);

            var response = await _service.LoginAsync(Request("comercio01", "correcta"));

            response.HasError.Should().BeTrue();
            response.Error.Should().Be(RoleNotAllowed);
        }

        [Theory]
        [InlineData(Roles.Administrador)]
        [InlineData(Roles.Cajero)]
        [InlineData(Roles.Cliente)]
        public async Task LoginAsync_WithAnAllowedRole_ShouldSignTheUserInAndReturnItsRoles(Roles role)
        {
            var user = IdentityMocks.BuildUser();
            GivenUser(user, passwordIsValid: true, role);

            var response = await _service.LoginAsync(Request("usuario01", "correcta"));

            response.HasError.Should().BeFalse();
            response.Id.Should().Be(user.Id);
            response.UserName.Should().Be(user.UserName);
            response.Roles.Should().ContainSingle().Which.Should().Be(role.ToString());
            _signInManager.Verify(m => m.SignInAsync(user, false, null), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_ShouldCloseTheActiveSession()
        {
            await _service.LogoutAsync();

            _signInManager.Verify(m => m.SignOutAsync(), Times.Once);
        }

        private static AuthenticationRequest Request(string userName, string password)
            => new() { UserName = userName, Password = password };

        private void GivenUser(ApplicationUser user, bool passwordIsValid, params Roles[] roles)
        {
            _userManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(passwordIsValid);
            _userManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(roles.Select(r => r.ToString()).ToList());
        }
    }
}
