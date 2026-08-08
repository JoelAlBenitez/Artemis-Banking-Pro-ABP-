<<<<<<< HEAD
using ArtemisBankingPro.IOC;
using ArtemisBankingPro.Infraestructrue.Identity.RegistrationAndConfiguration;
=======
>>>>>>> origin/development
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
<<<<<<< HEAD
    builder.Services.AddApplicationDependecies();
    builder.Services.AddInfraestructurePersistence(builder.Configuration);
    builder.Services.AddInfraestructureDependencies(builder.Configuration);
    builder.Services.AddWebApiIdentity(builder.Configuration);
=======
>>>>>>> origin/development

    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    var app = builder.Build();

<<<<<<< HEAD
    // Run Identity Seeds (Roles and Default Users)
    await app.Services.RunIdentitySeedsAsync();

=======
>>>>>>> origin/development
    // Configure the HTTP request pipeline.
    app.UseLoggingAndExceptionHandling();

    if (app.Environment.IsDevelopment())
    {
<<<<<<< HEAD
        app.MapOpenApi().AllowAnonymous();
=======
        app.MapOpenApi();
>>>>>>> origin/development
    }

    app.UseHttpsRedirection();

<<<<<<< HEAD
    app.UseAuthentication();
=======
>>>>>>> origin/development
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
