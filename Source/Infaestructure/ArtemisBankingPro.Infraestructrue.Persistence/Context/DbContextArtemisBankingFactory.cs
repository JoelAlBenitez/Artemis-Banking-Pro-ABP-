using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Context
{
    //Solo se usa en tiempo de diseño (dotnet ef migrations add / database update).
    //En ejecución el contexto lo registra AddInfraestructurePersistence leyendo DefaultConnection.
    public class DbContextArtemisBankingFactory : IDesignTimeDbContextFactory<DbContextArtemisBanking>
    {
        //Misma base que Identity: las tablas del banco y las de Identity conviven en
        //ArtemisBankingPro, cada contexto con su propia tabla de historial de migraciones.
        private const string DesignTimeConnection =
            "Server=localhost;Database=ArtemisBankingPro;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        public DbContextArtemisBanking CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DbContextArtemisBanking>();
            optionsBuilder.UseSqlServer(DesignTimeConnection, sql =>
            {
                sql.MigrationsAssembly(typeof(DbContextArtemisBanking).Assembly.FullName);
                sql.MigrationsHistoryTable("__EFMigrationsHistory_Persistence");
            });

            return new DbContextArtemisBanking(optionsBuilder.Options);
        }
    }
}
