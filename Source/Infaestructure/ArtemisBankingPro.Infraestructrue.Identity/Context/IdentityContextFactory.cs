using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ArtemisBankingPro.Infraestructrue.Identity.Context
{
    //Solo se usa en tiempo de diseño (dotnet ef migrations add / database update).
    //En ejecución el contexto lo registra GeneralConfiguration leyendo DefaultConnection.
    public class IdentityContextFactory : IDesignTimeDbContextFactory<IdentityContext>
    {
        //Misma base que el contexto de negocio: las tablas de Identity y las del banco conviven
        //en ArtemisBankingPro, cada contexto con su propia tabla de historial de migraciones.
        private const string DesignTimeConnection =
            "Server=localhost;Database=ArtemisBankingPro;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        public IdentityContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<IdentityContext>();
            optionsBuilder.UseSqlServer(DesignTimeConnection, sql =>
            {
                sql.MigrationsAssembly(typeof(IdentityContext).Assembly.FullName);
                sql.MigrationsHistoryTable("__EFMigrationsHistory_Identity");
            });

            return new IdentityContext(optionsBuilder.Options);
        }
    }
}
