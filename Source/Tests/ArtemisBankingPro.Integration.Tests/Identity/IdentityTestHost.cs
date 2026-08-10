using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Infraestructrue.Identity.Context;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using ArtemisBankingPro.Infraestructrue.Identity.Errors;
using ArtemisBankingPro.Infraestructrue.Identity.Mappings;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArtemisBankingPro.Integration.Tests.Identity
{
    //Levanta el stack real de ASP.NET Identity (UserManager, RoleManager, stores de EF y
    //proveedores de token) sobre una base en memoria aislada por prueba.
    //Limitaciones conocidas del proveedor en memoria: ignora el esquema "Identity" y el
    //índice único de IDCARD, por eso la unicidad de la cédula se valida en el servicio.
    internal sealed class IdentityTestHost : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;

        public UserManager<ApplicationUser> UserManager { get; }
        public RoleManager<IdentityRole> RoleManager { get; }
        public IdentityContext Context { get; }
        public IMapper Mapper { get; }

        public IdentityTestHost()
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddDataProtection();

            services.AddDbContext<IdentityContext>(options =>
                options.UseInMemoryDatabase($"identity-{Guid.NewGuid()}"));

            //Las mismas opciones que usa la aplicación: correo único y contraseñas simples
            services.Configure<IdentityOptions>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 1;
            });

            services.AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddErrorDescriber<SpanishIdentityErrorDescriber>()
                .AddEntityFrameworkStores<IdentityContext>()
                .AddDefaultTokenProviders();

            _provider = services.BuildServiceProvider();
            _scope = _provider.CreateScope();

            UserManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            RoleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            Context = _scope.ServiceProvider.GetRequiredService<IdentityContext>();

            Mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<IdentityProfile>(),
                NullLoggerFactory.Instance).CreateMapper();

            SeedRoles();
        }

        //Los cuatro roles del sistema. Comercio existe porque la Web API lo necesita, aunque
        //quede excluido de todos los servicios de mantenimiento.
        private void SeedRoles()
        {
            foreach (Roles role in Enum.GetValues<Roles>())
                RoleManager.CreateAsync(new IdentityRole(role.ToString())).GetAwaiter().GetResult();
        }

        public async Task<ApplicationUser> GivenUserAsync(
            Roles role,
            string userName,
            bool isActive = true,
            string? idCard = null,
            string? email = null,
            DateTimeOffset? createdAt = null,
            string firstName = "María",
            string lastName = "Gómez")
        {
            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email ?? $"{userName}@artemisbank.com",
                FirstName = firstName,
                LastName = lastName,
                IDCARD = idCard ?? Guid.NewGuid().ToString("N")[..11],
                IsActive = isActive,
                EmailConfirmed = isActive,
                CreatedAt = createdAt ?? DateTimeOffset.UtcNow
            };

            var result = await UserManager.CreateAsync(user, "Clave123*");
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"No fue posible crear el usuario de prueba: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            await UserManager.AddToRoleAsync(user, role.ToString());
            return user;
        }

        public void Dispose()
        {
            _scope.Dispose();
            _provider.Dispose();
        }
    }
}
