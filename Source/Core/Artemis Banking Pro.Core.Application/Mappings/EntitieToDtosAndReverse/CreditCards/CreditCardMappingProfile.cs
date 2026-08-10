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
                .ForMember(d => d.StateCreditCard, o => o.MapFrom(s => s.Status))
                .ForMember(d => d.ExpirationDate, o => o.MapFrom(s => s.ExpirationDate.ToString("MM/yy")))
                .ForMember(d => d.MaskedCardNumber, o => o.MapFrom(s => "**** **** **** " + s.LastFourDigits))
                .ForMember(d => d.FullNameCustomer, o => o.Ignore())
                .ReverseMap()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.StateCreditCard))
                .ForMember(d => d.AssignedByAdminId, o => o.Ignore());

            CreateMap<CardConsumption, CardConsumptionDto>()
                .ForMember(d => d.ConsumptionDate, o => o.MapFrom(s => s.CreatedAt))
                .ForMember(d => d.StateConsumption, o => o.MapFrom(s => s.Status))
                .ReverseMap()
                .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.ConsumptionDate))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.StateConsumption));
        }
    }
}
