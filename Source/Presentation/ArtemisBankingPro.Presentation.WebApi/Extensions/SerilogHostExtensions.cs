using ArtemisBankingPro.Presentation.WebApi.Middlewares;
using Serilog;

namespace ArtemisBankingPro.Presentation.WebApi.Extensions
{
   
    public static class SerilogHostExtensions
    {
        public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
        {
            builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext());

            return builder;
        }

        public static WebApplicationBuilder AddGlobalExceptionHandling(this WebApplicationBuilder builder)
        {
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

            return builder;
        }

        //Va lo más afuera posible del pipeline: cualquier fallo posterior queda capturado y con
        //identificador de correlación.
        public static WebApplication UseLoggingAndExceptionHandling(this WebApplication app)
        {
            app.UseCorrelationId();

            //El log de la petición envuelve al handler de excepciones, nunca al revés. Por dentro
            //vería la excepción antes de que el handler fije el código real y registraría un 500
            //en peticiones que terminan en 404, 400 o 409.
            app.UseSerilogRequestLogging(options =>
            {
                //El usuario se resuelve al autenticar, ya dentro de este middleware. Se enriquece
                //al cerrar la petición, que es cuando el evento se escribe.
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set(
                        UserContextLoggingMiddleware.UserLogPropertyName,
                        UserContextLoggingMiddleware.ResolveUserName(httpContext.User));

                    diagnosticContext.Set(
                        UserContextLoggingMiddleware.RoleLogPropertyName,
                        UserContextLoggingMiddleware.ResolveRoles(httpContext.User));
                };
            });

            app.UseExceptionHandler();

            return app;
        }

        //Se registra DESPUÉS de UseAuthentication: pone usuario y rol en el contexto de log para
        //todo lo que los handlers registren durante la petición.
        public static WebApplication UseRequestLoggingWithUserContext(this WebApplication app)
        {
            app.UseUserContextLogging();

            return app;
        }
    }
}
