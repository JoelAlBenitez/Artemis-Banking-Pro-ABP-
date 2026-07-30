using ArtemisBankingPro.Core.Domain.Interfaces.Generic;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBankingPro.IOC
{
    public static class InfraestructurePersistencesDependencies
    {
        public static IServiceCollection AddInfraestructurePersistence(this IServiceCollection services, IConfiguration configuration)
        {

            //configuration ef core memory  con fines de prueba la verdadera conexion esta comentada
            services.AddDbContext<DbContextArtemisBanking>(options =>
                options.UseInMemoryDatabase("MyDatabase")
            );

            //verdadera conexion sera descomentada cuando se han creadas las migraciones pertinentes
            /*services.AddDbContext<DbContextArtemisBanking>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")
            ));*/


            return services;
        }
    }
}
