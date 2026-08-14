using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using ArtemisBankingPro.Infraestructrue.Identity.Seeds;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBankingPro.Infraestructrue.Identity.RegistrationAndConfiguration
{
    public static class DataSeeds
    {
        public static async Task RunIdentitySeedsAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await DefaultRoles.SeedAsync(roleManager);
            await DefaultUsers.SeedAdminAsync(userManager);
            await DefaultUsers.SeedCajeroAsync(userManager);
            await DefaultUsers.SeedClienteAsync(userManager);
            await DefaultUsers.SeedAdminApiAsync(userManager);
            await DefaultUsers.SeedComercioApiAsync(userManager);
        }
    }
}
