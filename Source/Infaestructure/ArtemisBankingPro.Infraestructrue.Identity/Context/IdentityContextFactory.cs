using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ArtemisBankingPro.Infraestructrue.Identity.Context
{
    //Solo se usa en tiempo de diseño (dotnet ef migrations add / script).
    //En ejecución el contexto lo registra GeneralConfiguration, que hoy sigue apuntando a la
    //base en memoria hasta que se habilite la conexión real de SQL Server.
    public class IdentityContextFactory : IDesignTimeDbContextFactory<IdentityContext>
    {
        //La cadena no se conecta durante el scaffolding: EF solo necesita el proveedor para
        //saber qué tipos y qué SQL generar.
        private const string DesignTimeConnection =
            "Server=(localdb)\\MSSQLLocalDB;Database=ArtemisBankingProIdentity;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        public IdentityContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<IdentityContext>();
            optionsBuilder.UseSqlServer(DesignTimeConnection, sql =>
                sql.MigrationsAssembly(typeof(IdentityContext).Assembly.FullName));

            return new IdentityContext(optionsBuilder.Options);
        }
    }
}
