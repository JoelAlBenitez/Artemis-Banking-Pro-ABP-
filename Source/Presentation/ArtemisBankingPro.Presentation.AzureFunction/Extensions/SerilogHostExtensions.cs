using Microsoft.Azure.Functions.Worker.Builder;
using Serilog;

namespace ArtemisBankingPro.Presentation.AzureFunction.Extensions
{
    //Misma configuración de Serilog que la Web App y la Web API: se lee desde appsettings.json.
    public static class SerilogHostExtensions
    {
        public static FunctionsApplicationBuilder AddSerilogLogging(this FunctionsApplicationBuilder builder)
        {
            builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext());

            return builder;
        }
    }
}
