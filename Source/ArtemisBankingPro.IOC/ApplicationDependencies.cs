using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.Loans;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Loans;
using Artemis_Banking_Pro.Core.Application.Services.Loans;
using Artemis_Banking_Pro.Core.Application.Services.Loans.LoansValidate;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBankingPro.IOC
{
    public static class ApplicationDependencies
    {
        public static IServiceCollection AddApplicationDependecies(this IServiceCollection services)
        {

            #region Mappings
            services.AddAutoMapper(configuration =>
            {
                #region loans
                configuration.AddMaps(typeof(LoansMappingDtoToViewModelAndReverse).Assembly);
                configuration.AddMaps(typeof(LoansMappingEntitieToDtoAndReverse).Assembly);
                #endregion
            });

            #endregion

            #region loans
            services.AddScoped<ILoansServices, LoansServices>();
            services.AddScoped<ILoansValidateServices, LoansValidateServices>();
            services.AddScoped<IAmortizationCalculator, AmortizationCalculator>();
            services.AddScoped<ILoansOverdueServices, LoansOverdueServices>();
            #endregion



            return services;
        }
    }
}
