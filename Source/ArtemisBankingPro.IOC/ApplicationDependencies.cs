using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using Artemis_Banking_Pro.Core.Application.Services.CreditCards;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ArtemisBankingPro.IOC
{
    public static class ApplicationDependencies
    {
        public static IServiceCollection AddApplicationDependecies(this IServiceCollection services)
        {
            services.AddAutoMapper(configuration => { }, Assembly.GetAssembly(typeof(ICreditCardsServices))!);

            //Gestión de tarjetas de crédito
            services.AddScoped<ICreditCardsValidationServices, CreditCardsValidationServices>();
            services.AddScoped<ICreditCardsServices, CreditCardsServices>();

            return services;
        }
    }
}
