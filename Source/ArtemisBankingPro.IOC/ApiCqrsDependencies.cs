using Artemis_Banking_Pro.Core.Application.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ArtemisBankingPro.IOC
{
    public static class ApiCqrsDependencies
    {
        //Solo la Web API resuelve sus endpoints por Mediator. Registrar los handlers desde la
        //WebApp o desde Azure Functions arrastraría dependencias que solo existen en la API
        //—como el servicio de autenticación JWT— y el contenedor fallaría al construirse.
        public static IServiceCollection AddApiCqrsDependencies(this IServiceCollection services)
        {
            //Commands, Queries, handlers y validadores viven todos en la capa de aplicación:
            //un solo assembly alimenta el registro de los tres.
            var applicationAssembly = Assembly.GetAssembly(typeof(ValidationBehavior<,>))!;

            services.AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssembly(applicationAssembly);
                configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(applicationAssembly);

            return services;
        }
    }
}
