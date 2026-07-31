using ArtemisBankingPro.Core.Domain.Entities.Loans;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasSequence<Loan>("LoanNumberSequence")
                .StartsAt(100000000)
                .IncrementsBy(1);
            
            //en la feature pertinente agregar el sequence de cuentas de ahorro

            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

    }
}
