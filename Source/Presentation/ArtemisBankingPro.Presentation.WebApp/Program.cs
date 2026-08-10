using ArtemisBankingPro.IOC;
using ArtemisBankingPro.Presentation.WebApp.Extensions;
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
    builder.Services.AddControllersWithViews();

    builder.Services.AddApplicationDependecies();
    builder.Services.AddInfraestructurePersistence(builder.Configuration);
    builder.Services.AddInfraestructureDependencies(builder.Configuration);

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseLoggingAndExceptionHandling();

    if (!app.Environment.IsDevelopment())
    {
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthorization();

    // Pendiente: habilitar cuando exista ICurrentUserService (proyecto Identity).
    //app.UseUserContextLogging();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
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
