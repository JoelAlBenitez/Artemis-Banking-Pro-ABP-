using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Identity
{
    // Integration test explicito contra el IdentityContext / repositorio
    // Verifica que IdentityContext guarde y recupere usuarios y roles a nivel de base de datos.
    public sealed class IdentityContextTests : IDisposable
    {
        private readonly IdentityTestHost _host;

        public IdentityContextTests()
        {
            _host = new IdentityTestHost();
        }

        [Fact]
        public async Task IdentityContext_CanSaveAndRetrieveUsersAndRoles()
        {
            // Arrange
            var roleName = Roles.Cliente.ToString();
            var user = new ApplicationUser
            {
                UserName = "testuser",
                Email = "testuser@artemisbank.com",
                FirstName = "Test",
                LastName = "User",
                IDCARD = "00100000000",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Act: Guardar usuario via UserManager (que internamente usa IdentityContext)
            var createResult = await _host.UserManager.CreateAsync(user, "Clave123*");
            await _host.UserManager.AddToRoleAsync(user, roleName);

            // Assert: Recuperar directamente del IdentityContext
            var dbUser = await _host.Context.Users
                .FirstOrDefaultAsync(u => u.UserName == "testuser");

            dbUser.Should().NotBeNull();
            dbUser!.Email.Should().Be("testuser@artemisbank.com");
            dbUser.FirstName.Should().Be("Test");
            dbUser.LastName.Should().Be("User");
            dbUser.IDCARD.Should().Be("00100000000");

            var userRole = await _host.Context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == dbUser.Id);

            userRole.Should().NotBeNull();

            var role = await _host.Context.Roles
                .FirstOrDefaultAsync(r => r.Id == userRole!.RoleId);

            role.Should().NotBeNull();
            role!.Name.Should().Be(roleName);
        }

        [Fact]
        public async Task IdentityContext_CanSaveAndRetrieveAuthenticationTokens()
        {
            // Arrange
            var user = new ApplicationUser
            {
                UserName = "tokenuser",
                Email = "tokenuser@artemisbank.com",
                FirstName = "Token",
                LastName = "User",
                IDCARD = "00200000000",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _host.UserManager.CreateAsync(user, "Clave123*");

            // Act: Guardar token
            await _host.UserManager.SetAuthenticationTokenAsync(user, "TestProvider", "TestToken", "TestValue");

            // Assert: Recuperar token del IdentityContext
            var dbToken = await _host.Context.UserTokens
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.LoginProvider == "TestProvider" && t.Name == "TestToken");

            dbToken.Should().NotBeNull();
            dbToken!.Value.Should().Be("TestValue");
        }

        public void Dispose() => _host.Dispose();
    }
}
