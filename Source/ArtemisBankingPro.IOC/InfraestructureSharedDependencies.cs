using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using ArtemisBankingPro.Core.Domain.Settings.Email;
using ArtemisBankingPro.Infraestructrue.Shared.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBankingPro.IOC
{
    public static class InfraestructureSharedDependencies
    {
        public static IServiceCollection AddInfraestructureDependencies(this IServiceCollection services,
            IConfiguration configuration
            ) {

            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.AddScoped<IEmailServices, EmailServices>();
            return services;
        }
    }
}
