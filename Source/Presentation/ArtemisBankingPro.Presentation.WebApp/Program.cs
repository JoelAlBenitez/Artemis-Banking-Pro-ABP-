using ArtemisBankingPro.IOC;
using ArtemisBankingPro.Infraestructrue.Identity.RegistrationAndConfiguration;
using ArtemisBankingPro.Presentation.WebApp.Extensions;
using ArtemisBankingPro.Presentation.WebApp.Middlewares;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddSerilogLogging();
    builder.AddGlobalExceptionHandling();

    // Register all dependencies
    builder.Services.AddApplicationDependecies();
    builder.Services.AddInfraestructurePersistence(builder.Configuration);
    builder.Services.AddInfraestructureDependencies(builder.Configuration);
    builder.Services.AddWebAppIdentity(builder.Configuration);

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    var app = builder.Build();

    // Run Identity Seeds (Roles and Default Users)
    await app.Services.RunIdentitySeedsAsync();

    // Configure the HTTP request pipeline.
    app.UseLoggingAndExceptionHandling();

    if (!app.Environment.IsDevelopment())
    {
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthentication();

    // Entre autenticación y autorización: el usuario ya está resuelto y el rechazo por rol
    // también queda registrado con su nombre y su rol.
    app.UseRequestLoggingWithUserContext();

    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Account}/{action=Login}/{id?}")
        .WithStaticAssets();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La Web App finalizó de forma inesperada durante el arranque.");
}
finally
{
    Log.CloseAndFlush();
}
