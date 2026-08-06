using ArtemisBankingPro.Core.Domain.Common.Enum;
using Microsoft.AspNetCore.Identity;

namespace ArtemisBankingPro.Infraestructrue.Identity.Seeds
{
    public static class DefaultRoles
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { 
                Roles.Administrador.ToString(), 
                Roles.Cajero.ToString(), 
                Roles.Cliente.ToString(), 
                Roles.Comercio.ToString() 
            };

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
