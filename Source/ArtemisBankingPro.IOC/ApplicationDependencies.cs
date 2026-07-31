using Microsoft.Extensions.DependencyInjection;
using Artemis_Banking_Pro.Core.Application.Contracts.Dashboard;
using Artemis_Banking_Pro.Core.Application.Services.Dashboard;

namespace ArtemisBankingPro.IOC
{
    public static class ApplicationDependencies
    {
        public static IServiceCollection AddApplicationDependecies(this IServiceCollection services)
        {
            services.AddTransient<IDashboardService, DashboardService>();

            return services;
        }
    }
}
