using ArtemisBankingPro.Core.Application.Contracts.Users.Tokens;
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
    //Login de la Web API. Solo Administrador y Comercio pueden obtener un JWT.
    public sealed class AuthWebApiServiceTests
    {
        private const string InvalidCredentials = "Los datos de acceso son inválidos.";
        private const string AccessDenied = "Acceso denegado. No tiene permisos para utilizar este recurso.";
        private const string InactiveAccount = "Su cuenta se encuentra inactiva. Debe activar su cuenta antes de iniciar sesión.";

        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator;
        private readonly AuthWebApiService _service;

        public AuthWebApiServiceTests()
        {
            _userManager = IdentityMocks.UserManager();
            _jwtTokenGenerator = new Mock<IJwtTokenGenerator>();

            _jwtTokenGenerator
                .Setup(g => g.GenerateJwtTokenAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<string>>()))
                .ReturnsAsync(new JwtResponseDto
                {
                    Token = "jwt-de-prueba",
                    Expiration = DateTime.UtcNow.AddMinutes(30)
                });

            _service = new AuthWebApiService(
                _userManager.Object,
                _jwtTokenGenerator.Object,
                NullLogger<AuthWebApiService>.Instance);
        }

        [Fact]
        public async Task LoginAsync_WithUnknownUserName_ShouldReturnTheInvalidCredentialsMessage()
        {
            _userManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            var response = await _service.LoginAsync(Request("noexiste", "clave"));

            response.HasError.Should().BeTrue();
            response.Forbidden.Should().BeFalse();
            response.Error.Should().Be(InvalidCredentials);
        }

        //Un 403 solo puede emitirse ante credenciales válidas: si la contraseña es incorrecta
        //la respuesta no debe delatar el rol del usuario.
        [Fact]
        public async Task LoginAsync_WithWrongPassword_ShouldNotReturnForbidden()
        {
            GivenUser(IdentityMocks.BuildUser(), passwordIsValid: false, Roles.Cajero);

            var response = await _service.LoginAsync(Request("cajero01", "incorrecta"));

            response.Forbidden.Should().BeFalse();
            response.Error.Should().Be(InvalidCredentials);
        }

        [Theory]
        [InlineData(Roles.Cajero)]
        [InlineData(Roles.Cliente)]
        public async Task LoginAsync_WithARoleThatDoesNotBelongToTheApi_ShouldReturnForbidden(Roles role)
        {
            GivenUser(IdentityMocks.BuildUser(), passwordIsValid: true, role);

            var response = await _service.LoginAsync(Request("usuario01", "correcta"));

            response.HasError.Should().BeTrue();
            response.Forbidden.Should().BeTrue();
            response.Error.Should().Be(AccessDenied);
        }

        [Fact]
        public async Task LoginAsync_WithAnInactiveAccount_ShouldNotGenerateAToken()
        {
            GivenUser(IdentityMocks.BuildUser(isActive: false), passwordIsValid: true, Roles.Administrador);

            var response = await _service.LoginAsync(Request("adminapi", "correcta"));

            response.HasError.Should().BeTrue();
            response.Error.Should().Be(InactiveAccount);
            _jwtTokenGenerator.Verify(g => g.GenerateJwtTokenAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<string>>()), Times.Never);
        }

        [Theory]
        [InlineData(Roles.Administrador)]
        [InlineData(Roles.Comercio)]
        public async Task LoginAsync_WithAnAllowedRole_ShouldReturnTheGeneratedToken(Roles role)
        {
            var user = IdentityMocks.BuildUser();
            GivenUser(user, passwordIsValid: true, role);

            var response = await _service.LoginAsync(Request("usuario01", "correcta"));

            response.HasError.Should().BeFalse();
            response.Token.Should().Be("jwt-de-prueba");
            response.UserName.Should().Be(user.UserName);
            response.Roles.Should().ContainSingle().Which.Should().Be(role.ToString());
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
