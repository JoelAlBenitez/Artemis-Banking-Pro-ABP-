using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.SavingsAccounts;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.Beneficiaries;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Commerces;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Loans;
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
            //Identity y el contexto de negocio comparten base de datos, así que cada uno lleva su
            //propia tabla de historial: si compartieran una sola, las migraciones de un contexto
            //aparecerían como desconocidas para el otro.
            services.AddDbContext<DbContextArtemisBanking>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql =>
                    {
                        sql.MigrationsAssembly(typeof(DbContextArtemisBanking).Assembly.FullName);
                        sql.MigrationsHistoryTable("__EFMigrationsHistory_Persistence");

                        //Un servidor local lento agota el tiempo de espera y varios módulos
                        //muestran error a la vez, como si el sistema estuviera caído. El reintento
                        //solo cubre fallos transitorios: un error de negocio nunca se reintenta.
                        sql.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null);
                        sql.CommandTimeout(60);
                    })
            );

            //Gestión de tarjetas de crédito
            #region credit cards
            services.AddScoped<ICreditCardsRepository, CreditCardsRepository>();
            services.AddScoped<ICardConsumptionRepository, CardConsumptionRepository>();
            #endregion

            #region loans
            services.AddScoped<ILoansRepository, LoansRepository>();
            services.AddScoped<ILoanInstallmentRepository, LoanInstallmentRepository>();
            #endregion

            //Gestión de cuentas de ahorro
            #region savings accounts
            services.AddScoped<ISavingsAccountsRepository, SavingsAccountsRepository>();
            #endregion


            services.AddScoped<ICardPaymentRepository, CardPaymentRepository>();

            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<ICashAdvanceRepository, CashAdvanceRepository>();

            services.AddScoped<IBeneficiaryRepository, BeneficiaryRepository>();

            //Comercios y procesador de pagos Hermes Pay
            services.AddScoped<ICommerceRepository, CommerceRepository>();
            services.AddScoped<ICommercePaymentRepository, CommercePaymentRepository>();

            services.AddScoped<ILoansPaymentRepository, LoansPaymentRepository>();

            return services;
        }
    }
}
