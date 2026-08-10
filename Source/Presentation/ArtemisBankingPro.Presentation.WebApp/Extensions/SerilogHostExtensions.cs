using ArtemisBankingPro.Presentation.WebApp.Middlewares;
using Serilog;

namespace ArtemisBankingPro.Presentation.WebApp.Extensions
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
            app.UseExceptionHandler();
            app.UseCorrelationId();

            return app;
        }

        //Se registra DESPUÉS de UseAuthentication. El log de la petición es el que documenta
        //endpoint y resultado, así que debe caer dentro del contexto de usuario: si se registra
        //antes de autenticar, los campos obligatorios de usuario y rol salen siempre vacíos.
        public static WebApplication UseRequestLoggingWithUserContext(this WebApplication app)
        {
            app.UseUserContextLogging();
            app.UseSerilogRequestLogging();

            return app;
        }
    }
}
