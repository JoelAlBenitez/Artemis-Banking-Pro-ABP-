using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.Contracts.Users.Password;
using ArtemisBankingPro.Core.Application.Contracts.Users.ExternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.InternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.Tokens;
using ArtemisBankingPro.Infraestructrue.Identity.Context;
using ArtemisBankingPro.Infraestructrue.Identity.Mappings;
using ArtemisBankingPro.Infraestructrue.Identity.Services.Management;
using ArtemisBankingPro.Infraestructrue.Identity.Services.Password;
using ArtemisBankingPro.Infraestructrue.Identity.Services.Registration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBankingPro.Infraestructrue.Identity.RegistrationAndConfiguration
{
    public static class GeneralConfiguration
    {
        public static void AddGeneralConfiguration(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<IdentityContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql =>
                    {
                        sql.MigrationsAssembly(typeof(IdentityContext).Assembly.FullName);
                        sql.MigrationsHistoryTable("__EFMigrationsHistory_Identity");
                    })
            );

            services.AddAutoMapper(config => config.AddProfile<IdentityProfile>());

            services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
            services.AddScoped<IAccountRegistrationService, AccountRegistrationService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<ArtemisBankingPro.Infraestructrue.Identity.Interfaces.IGenerateTokens, ArtemisBankingPro.Infraestructrue.Identity.Services.Tokens.GenerateTokens>();
        }
    }
}

