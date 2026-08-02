using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using ArtemisBankingPro.Core.Domain.Settings.Email;
using ArtemisBankingPro.Infraestructrue.Shared.Services.Email;
using ArtemisBankingPro.Infraestructrue.Shared.Services.Generators;
using ArtemisBankingPro.Infraestructrue.Shared.Services.Security;
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

            //Generación y hashing de los datos sensibles de la tarjeta de crédito
            services.AddScoped<ICardNumberGenerator, CardNumberGenerator>();
            services.AddSingleton<ICvcHasher, CvcHasher>();

            return services;
        }
    }
}
