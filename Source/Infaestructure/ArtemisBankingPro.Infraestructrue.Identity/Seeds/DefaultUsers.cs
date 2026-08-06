using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace ArtemisBankingPro.Infraestructrue.Identity.Seeds
{
    public static class DefaultUsers
    {
        public static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
        {
            var defaultUser = new ApplicationUser
            {
                UserName = "adminuser",
                Email = "adrixndeveloper29@gmail.com",
                FirstName = "Admin",
                LastName = "Default",
                IDCARD = "00100000001",
                IsActive = true,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            if (userManager.Users.All(u => u.Email != defaultUser.Email))
            {
                var user = await userManager.FindByEmailAsync(defaultUser.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(defaultUser, "Admin123*");
                    await userManager.AddToRoleAsync(defaultUser, Roles.Administrador);
                }
            }
        }

        public static async Task SeedCajeroAsync(UserManager<ApplicationUser> userManager)
        {
            var defaultUser = new ApplicationUser
            {
                UserName = "cajerouser",
                Email = "cajero@artemisbank.com",
                FirstName = "Cajero",
                LastName = "Default",
                IDCARD = "00200000002",
                IsActive = true,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            if (userManager.Users.All(u => u.Email != defaultUser.Email))
            {
                var user = await userManager.FindByEmailAsync(defaultUser.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(defaultUser, "Cajero123*");
                    await userManager.AddToRoleAsync(defaultUser, Roles.Cajero);
                }
            }
        }

        public static async Task SeedClienteAsync(UserManager<ApplicationUser> userManager)
        {
            var defaultUser = new ApplicationUser
            {
                UserName = "clienteuser",
                Email = "cliente@artemisbank.com",
                FirstName = "Cliente",
                LastName = "Default",
                IDCARD = "00300000003",
                IsActive = true,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            if (userManager.Users.All(u => u.Email != defaultUser.Email))
            {
                var user = await userManager.FindByEmailAsync(defaultUser.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(defaultUser, "Cliente123*");
                    await userManager.AddToRoleAsync(defaultUser, Roles.Cliente);
                }
            }
        }

        public static async Task SeedAdminApiAsync(UserManager<ApplicationUser> userManager)
        {
            var defaultUser = new ApplicationUser
            {
                UserName = "adminapi",
                Email = "adminapi@artemisbank.com",
                FirstName = "Usuario administrador",
                LastName = "de API",
                IDCARD = "00400000004",
                IsActive = true,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            if (userManager.Users.All(u => u.Email != defaultUser.Email))
            {
                var user = await userManager.FindByEmailAsync(defaultUser.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(defaultUser, "AdminApi123*");
                    await userManager.AddToRoleAsync(defaultUser, Roles.Administrador);
                }
            }
        }

        public static async Task SeedComercioApiAsync(UserManager<ApplicationUser> userManager)
        {
            var defaultUser = new ApplicationUser
            {
                UserName = "comercioapi",
                Email = "comercioapi@artemisbank.com",
                FirstName = "Usuario comercio",
                LastName = "de API",
                IDCARD = "00500000005",
                IsActive = true,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            if (userManager.Users.All(u => u.Email != defaultUser.Email))
            {
                var user = await userManager.FindByEmailAsync(defaultUser.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(defaultUser, "ComercioApi123*");
                    await userManager.AddToRoleAsync(defaultUser, Roles.Comercio);
                }
            }
        }
    }
}
