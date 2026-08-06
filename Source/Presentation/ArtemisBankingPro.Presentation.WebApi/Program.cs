using ArtemisBankingPro.IOC;
using ArtemisBankingPro.Infraestructrue.Identity.RegistrationAndConfiguration;
using ArtemisBankingPro.Presentation.WebApi.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddSerilogLogging();
    builder.AddGlobalExceptionHandling();

    // Add services to the container.
    builder.Services.AddApplicationDependecies();
    builder.Services.AddInfraestructurePersistence(builder.Configuration);
    builder.Services.AddInfraestructureDependencies(builder.Configuration);
    builder.Services.AddWebApiIdentity(builder.Configuration);

    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // Run Identity Seeds (Roles and Default Users)
    await app.Services.RunIdentitySeedsAsync();

    // Configure the HTTP request pipeline.
    app.UseLoggingAndExceptionHandling();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().AllowAnonymous();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    // Pendiente: habilitar cuando exista ICurrentUserService (proyecto Identity).
    //app.UseUserContextLogging();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La Web API finalizó de forma inesperada durante el arranque.");
}
finally
{
    Log.CloseAndFlush();
}
