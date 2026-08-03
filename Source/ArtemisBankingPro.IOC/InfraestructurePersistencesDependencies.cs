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
            //configuration ef core memory  con fines de prueba la verdadera conexion esta comentada
            services.AddDbContext<DbContextArtemisBanking>(options =>
                options.UseInMemoryDatabase("MyDatabase")
            );

            //verdadera conexion sera descomentada cuando se han creadas las migraciones pertinentes
            /*services.AddDbContext<DbContextArtemisBanking>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")
            ));*/

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
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

            services.AddScoped<ICardPaymentRepository, CardPaymentRepository>();

            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<ICashAdvanceRepository, CashAdvanceRepository>();

            services.AddScoped<IBeneficiaryRepository, BeneficiaryRepository>();

            services.AddScoped<ILoansPaymentRepository, LoansPaymentRepository>();

            return services;
        }
    }
}
