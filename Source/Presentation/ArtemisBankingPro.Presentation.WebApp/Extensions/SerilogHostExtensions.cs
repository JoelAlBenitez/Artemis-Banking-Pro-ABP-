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

        
        public static WebApplication UseLoggingAndExceptionHandling(this WebApplication app)
        {
            app.UseExceptionHandler();
            app.UseCorrelationId();
            app.UseSerilogRequestLogging();

            return app;
        }
    }
}
