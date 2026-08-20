using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Entities.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Context
{
    public class DbContextArtemisBanking : DbContext
    {
        public DbContextArtemisBanking(DbContextOptions<DbContextArtemisBanking> options) : base(options) { }

        //Gestión de préstamos
        public DbSet<Loan> Loans { get; set; }
        public DbSet<LoanInstallment> LoanInstallments {  get; set; }
        public DbSet<LoanPayment> LoanPayments {  get; set; }

        //Gestión de tarjetas de crédito
        public DbSet<CreditCard> CreditCards { get; set; }
        public DbSet<CardConsumption> CardConsumptions { get; set; }
        public DbSet<CardPayment> CardPayments { get; set; }

        //Gestión de cuentas de ahorro
        public DbSet<SavingsAccount> SavingsAccounts { get; set; }

        // Transacciones y Avances
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<CashAdvance> CashAdvances { get; set; }

        // Beneficiarios
        public DbSet<Beneficiary> Beneficiaries { get; set; }

        //Comercios y procesador de pagos Hermes Pay
        public DbSet<Commerce> Commerces { get; set; }
        public DbSet<CommercePayment> CommercePayments { get; set; }

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
