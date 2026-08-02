using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
﻿using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.CreditCards;
using Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.Loans;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.CreditCards;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Loans;
using Artemis_Banking_Pro.Core.Application.Services.CreditCards;
using Artemis_Banking_Pro.Core.Application.Services.Loans;
using Artemis_Banking_Pro.Core.Application.Services.Loans.LoansValidate;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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
            });

            #endregion

            #region loans
            services.AddScoped<ILoansServices, LoansServices>();
            services.AddScoped<ILoansValidateServices, LoansValidateServices>();
            services.AddScoped<IAmortizationCalculator, AmortizationCalculator>();
            services.AddScoped<ILoansOverdueServices, LoansOverdueServices>();
            #endregion

            #region credit cards
            services.AddScoped<ICreditCardsServices, CreditCardsServices>();
            services.AddScoped<ICreditCardsValidationServices, CreditCardsValidationServices>();
            #endregion



            return services;
        }
    }
}
