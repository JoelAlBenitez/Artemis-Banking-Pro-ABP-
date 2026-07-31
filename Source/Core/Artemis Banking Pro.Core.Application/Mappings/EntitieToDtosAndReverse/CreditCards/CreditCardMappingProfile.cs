using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.CreditCards
{
    public sealed class CreditCardMappingProfile : Profile
    {
        public CreditCardMappingProfile()
        {
            CreateMap<CreditCard, CreditCardDto>()
                .ReverseMap();

            CreateMap<CardConsumption, CardConsumptionDto>()
                .ReverseMap();
        }
    }
}
