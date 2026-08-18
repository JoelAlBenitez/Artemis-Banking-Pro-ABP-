using System;
using System.Threading.Tasks;
using ArtemisBankingPro.Infraestructrue.Identity.RegistrationAndConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBankingPro.IOC
{
    public static class IdentityDependencies
    {
        public static IServiceCollection AddWebApiIdentityDependencies(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.AddWebApiIdentity(configuration);
            return services;
        }

        public static IServiceCollection AddWebAppIdentityDependencies(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.AddWebAppIdentity(configuration);
            return services;
        }

        public static async Task RunIdentitySeedsAsync(this IServiceProvider serviceProvider)
        {
            await DataSeeds.RunIdentitySeedsAsync(serviceProvider);
        }
    }
}
