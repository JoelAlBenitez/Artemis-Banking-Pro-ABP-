using ArtemisBankingPro.IOC;
using ArtemisBankingPro.Presentation.AzureFunction.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = FunctionsApplication.CreateBuilder(args);

    builder.ConfigureFunctionsWebApplication();

    builder.Services
        .AddApplicationInsightsTelemetryWorkerService()
        .ConfigureFunctionsApplicationInsights();

    builder.AddSerilogLogging();

    builder.Services.AddApplicationDependecies();
    builder.Services.AddInfraestructurePersistence(builder.Configuration);
    builder.Services.AddInfraestructureDependencies(builder.Configuration);

    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "El proceso de Azure Functions finalizó de forma inesperada durante el arranque.");
}
finally
{
    await Log.CloseAndFlushAsync();
}
