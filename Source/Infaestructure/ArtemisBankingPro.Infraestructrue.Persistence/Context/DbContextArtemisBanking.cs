using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Context
{
    public class DbContextArtemisBanking : DbContext
    {
        public DbContextArtemisBanking(DbContextOptions<DbContextArtemisBanking> options) : base(options) { }

        //add db set here and add comment, example

        //funcionality .....
        ///
        ////
        ///

        //Gestión de préstamos
        public DbSet<Loan> Loans { get; set; }
        public DbSet<LoanInstallment> LoanInstallments {  get; set; }
        public DbSet<LoanPayment> LoanPayments {  get; set; }

        //Gestión de tarjetas de crédito
        public DbSet<CreditCard> CreditCards { get; set; }
        public DbSet<CardConsumption> CardConsumptions { get; set; }

        //Gestión de cuentas de ahorro
        public DbSet<SavingsAccount> SavingsAccounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //El genérico es el tipo de valor que emite la secuencia, no la entidad
            modelBuilder.HasSequence<int>("LoanNumberSequence")
                .StartsAt(100000000)
                .IncrementsBy(1);
            
            //Las cuentas de ahorro no usan secuencia: su número de 9 dígitos se genera con
            //reintento acotado verificando simultáneamente cuentas y préstamos, porque ambos
            //comparten el mismo espacio de numeración y una secuencia propia colisionaría
            //con LoanNumberSequence. Ver IAccountNumberGenerator.

            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

    }
}
