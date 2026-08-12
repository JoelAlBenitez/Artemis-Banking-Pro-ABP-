using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Commerces;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.CreditCards;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Loans;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Users;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArtemisBankingPro.Unit.Tests.Features
{
    //Los handlers de la Web API proyectan sus respuestas con AutoMapper. Las pruebas usan los
    //perfiles reales: un mapeo mal declarado debe romper la prueba del handler, no pasar
    //inadvertido hasta la respuesta HTTP.
    internal static class ApiMapperFactory
    {
        public static MapperConfiguration BuildConfiguration()
            => new(configuration =>
            {
                configuration.AddProfile<UsersMappingEntitieToDtoAndReverse>();
                configuration.AddProfile<CommercesMappingEntitieToDtoAndReverse>();
                configuration.AddProfile<SavingsAccountsApiMapping>();
                configuration.AddProfile<LoansApiMapping>();
                configuration.AddProfile<CreditCardsApiMapping>();
            }, NullLoggerFactory.Instance);

        public static IMapper Create() => BuildConfiguration().CreateMapper();
    }
}
