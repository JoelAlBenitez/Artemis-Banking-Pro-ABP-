using ArtemisBankingPro.Core.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace ArtemisBankingPro.Infraestructrue.Identity.Seeds
{
    public static class DefaultRoles
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { Roles.Administrador, Roles.Cajero, Roles.Cliente, Roles.Comercio };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}
