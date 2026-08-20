using ArtemisBankingPro.Core.Domain.Common.Enum;
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
                    await userManager.AddToRoleAsync(defaultUser, Roles.Administrador.ToString());
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
                    await userManager.AddToRoleAsync(defaultUser, Roles.Cajero.ToString());
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
                    await userManager.AddToRoleAsync(defaultUser, Roles.Cliente.ToString());
                }
            }

            var prueba1 = new ApplicationUser
            {
                UserName = "prueba1",
                Email = "prueba1@artemisbank.com",
                FirstName = "Prueba",
                LastName = "Uno",
                IDCARD = "00300000010",
                IsActive = true,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };
            if (userManager.Users.All(u => u.Email != prueba1.Email))
            {
                var user = await userManager.FindByEmailAsync(prueba1.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(prueba1, "Prueba123*");
                    await userManager.AddToRoleAsync(prueba1, Roles.Cliente.ToString());
                }
            }

            var prueba2 = new ApplicationUser
            {
                UserName = "prueba2",
                Email = "prueba2@artemisbank.com",
                FirstName = "Prueba",
                LastName = "Dos",
                IDCARD = "00300000011",
                IsActive = true,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };
            if (userManager.Users.All(u => u.Email != prueba2.Email))
            {
                var user = await userManager.FindByEmailAsync(prueba2.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(prueba2, "Prueba123*");
                    await userManager.AddToRoleAsync(prueba2, Roles.Cliente.ToString());
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
                    await userManager.AddToRoleAsync(defaultUser, Roles.Administrador.ToString());
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
                    await userManager.AddToRoleAsync(defaultUser, Roles.Comercio.ToString());
                }
            }
        }
    }
}
