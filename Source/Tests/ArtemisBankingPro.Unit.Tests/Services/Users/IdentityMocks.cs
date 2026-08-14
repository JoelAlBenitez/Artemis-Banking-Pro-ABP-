using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace ArtemisBankingPro.Unit.Tests.Services.Users
{
    //UserManager y SignInManager son clases concretas con constructores largos. Moq puede
    //interceptarlas porque sus miembros son virtuales, pero hay que pasarles el store y
    //dejar el resto en null: ninguna de las pruebas ejecuta el comportamiento base.
    internal static class IdentityMocks
    {
        internal static Mock<UserManager<ApplicationUser>> UserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();

            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        internal static Mock<SignInManager<ApplicationUser>> SignInManager(
            Mock<UserManager<ApplicationUser>> userManager)
        {
            return new Mock<SignInManager<ApplicationUser>>(
                userManager.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                null!,
                null!,
                Mock.Of<IAuthenticationSchemeProvider>(),
                null!);
        }

        internal static ApplicationUser BuildUser(
            string id = "user-1",
            string userName = "usuario01",
            string email = "usuario01@artemisbank.com",
            bool isActive = true)
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = userName,
                Email = email,
                FirstName = "María",
                LastName = "Gómez",
                IDCARD = "00187654321",
                IsActive = isActive,
                EmailConfirmed = isActive,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
