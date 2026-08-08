using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
﻿using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.CreditCards;
using Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.Loans;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.CreditCards;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Loans;
using Artemis_Banking_Pro.Core.Application.Services.CreditCards;
using Artemis_Banking_Pro.Core.Application.Services.Loans;
using Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Services.Loans.LoansValidate;
using Artemis_Banking_Pro.Core.Application.Services.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Services.SavingsAccounts.SavingsAccountsValidate;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Artemis_Banking_Pro.Core.Application.Contracts.Dashboard;
using Artemis_Banking_Pro.Core.Application.Services.Dashboard;
using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.Services.Transactions;
using Artemis_Banking_Pro.Core.Application.Contracts.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.Services.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.Contracts.Debts;
using Artemis_Banking_Pro.Core.Application.Services.Debts;

namespace ArtemisBankingPro.IOC
{
    public static class ApplicationDependencies
    {
        public static IServiceCollection AddApplicationDependecies(this IServiceCollection services)
        {
            services.AddAutoMapper(configuration => { }, Assembly.GetAssembly(typeof(ICreditCardsServices))!);

           

            #region Mappings
            services.AddAutoMapper(configuration =>
            {
                #region loans
                configuration.AddMaps(typeof(LoansMappingDtoToViewModelAndReverse).Assembly);
                configuration.AddMaps(typeof(LoansMappingEntitieToDtoAndReverse).Assembly);
                #endregion

                #region credit cards
                configuration.AddMaps(typeof(CreditCardsMappingDtoToViewModelAndReverse).Assembly);
                configuration.AddMaps(typeof(CreditCardsMappingEntitieToDtoAndReverse).Assembly);
                #endregion

                #region savings accounts
                configuration.AddMaps(typeof(SavingsAccountsMappingDtoToViewModelAndReverse).Assembly);
                configuration.AddMaps(typeof(SavingsAccountsMappingEntitieToDtoAndReverse).Assembly);
                #endregion

                #region customer dashboard
                configuration.AddMaps(typeof(SavingsAccountMappingProfile).Assembly);
                #endregion
            });

          

            #region loans
            services.AddScoped<ILoansServices, LoansServices>();
            services.AddScoped<ILoansValidateServices, LoansValidateServices>();
            services.AddScoped<IAmortizationCalculator, AmortizationCalculator>();
            services.AddScoped<ILoansOverdueServices, LoansOverdueServices>();
            #endregion


            //Compartido: préstamos, tarjetas y dashboard consumen el mismo cálculo de deuda
            services.AddScoped<IDebtCalculator, DebtCalculator>();

            #region credit cards
            services.AddScoped<ICreditCardsServices, CreditCardsServices>();
            services.AddScoped<ICreditCardsValidationServices, CreditCardsValidationServices>();
            #endregion

            #region savings accounts
            services.AddScoped<ISavingsAccountsServices, SavingsAccountsServices>();
            services.AddScoped<ISavingsAccountsValidateServices, SavingsAccountsValidateServices>();
            #endregion


            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ITransactionsValidationServices, TransactionsValidationServices>();

            services.AddScoped<IBeneficiaryServices, BeneficiaryServices>();
            services.AddScoped<IBeneficiaryValidationServices, BeneficiaryValidationServices>();
            services.AddScoped<ICashAdvanceServices, CashAdvanceServices>();
            services.AddScoped<ICashAdvanceValidationServices, CashAdvanceValidationServices>();

            return services;
        }
    }
}
