using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Settings;
using ArtemisBankingPro.Infraestructrue.Identity.Services.Tokens;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Users
{
    //El JWT debe incluir como mínimo identificador, nombre de usuario, rol, fecha de emisión
    //y fecha de expiración.
    public sealed class JwtTokenGeneratorTests
    {
        private readonly JwtSettings _settings = new()
        {
            Key = "clave-de-pruebas-artemis-banking-pro-con-longitud-suficiente",
            Issuer = "ArtemisBankingPro",
            Audience = "ArtemisBankingProUsers",
            DurationInMinutes = 30
        };

        private readonly JwtTokenGenerator _generator;

        public JwtTokenGeneratorTests()
        {
            _generator = new JwtTokenGenerator(
                Options.Create(_settings),
                NullLogger<JwtTokenGenerator>.Instance);
        }

        [Fact]
        public async Task GenerateJwtTokenAsync_ShouldIncludeTheMinimumClaimsRequiredByTheDocument()
        {
            var response = await _generator.GenerateJwtTokenAsync(
                "user-1", "admin@artemis.com", "adminapi", new List<string> { nameof(Roles.Administrador) });

            var token = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);

            token.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user-1");
            token.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "adminapi");
            token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == nameof(Roles.Administrador));
            token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
            token.Issuer.Should().Be(_settings.Issuer);
            token.Audiences.Should().Contain(_settings.Audience);
        }

        [Fact]
        public async Task GenerateJwtTokenAsync_ShouldExpireAfterTheConfiguredDuration()
        {
            var before = DateTime.UtcNow;

            var response = await _generator.GenerateJwtTokenAsync(
                "user-1", "admin@artemis.com", "adminapi", new List<string> { nameof(Roles.Administrador) });

            response.Expiration.Should().BeCloseTo(before.AddMinutes(_settings.DurationInMinutes), TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task GenerateJwtTokenAsync_WithSeveralRoles_ShouldIncludeAllOfThem()
        {
            var response = await _generator.GenerateJwtTokenAsync(
                "user-1", "admin@artemis.com", "adminapi",
                new List<string> { nameof(Roles.Administrador), nameof(Roles.Comercio) });

            var token = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);

            token.Claims.Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .Should().BeEquivalentTo(nameof(Roles.Administrador), nameof(Roles.Comercio));
        }
    }
}
