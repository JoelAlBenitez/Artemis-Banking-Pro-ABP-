using ArtemisBankingPro.Core.Domain.Common.Constants;
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
            //Préstamos y cuentas de ahorro comparten el espacio de 9 dígitos. Cada producto emite
            //su número desde su propia secuencia y los rangos son disjuntos: la base de datos
            //garantiza que un número de préstamo nunca pueda repetirse como número de cuenta,
            //sin recorrer un solo registro.

            //El genérico es el tipo de valor que emite la secuencia, no la entidad
            modelBuilder.HasSequence<int>("LoanNumberSequence")
                .StartsAt(DomainConstants.LoanNumberSequenceStart)
                .IncrementsBy(1)
                .HasMax(DomainConstants.LoanNumberSequenceMax);

            modelBuilder.HasSequence<int>("SavingsAccountNumberSequence")
                .StartsAt(DomainConstants.AccountNumberSequenceStart)
                .IncrementsBy(1)
                .HasMax(DomainConstants.AccountNumberSequenceMax);

            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

    }
}
