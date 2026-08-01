using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using Artemis_Banking_Pro.Core.Application.ViewModels.CreditCards;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.CreditCards
{
    public sealed class CreditCardsMappingDtoToViewModelAndReverse : Profile
    {
        public CreditCardsMappingDtoToViewModelAndReverse()
        {
            CreateMap<ClientCreditCardDto, ClientCreditCardViewModel>().ReverseMap();
            CreateMap<CreditCardAssignmentDto, CreditCardAssignmentViewModel>().ReverseMap();
            CreateMap<CreditCardFilterDto, CreditCardFilterViewModel>().ReverseMap();

            CreateMap<EditCardLimitDto, EditCardLimitViewModel>()
                .ForMember(d => d.CreditCardId, o => o.MapFrom(s => s.Id))
                .ReverseMap()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.CreditCardId));

            CreateMap<CreditCardDto, CreditCardViewModel>()
                .ForMember(d => d.StateCreditCard,
                    o => o.MapFrom(s => s.StateCreditCard == CreditCardStatus.Activa ? "Activa" : "Cancelada"));

            CreateMap<CreditCardDto, DetailsCreditCardViewModel>()
                .ForMember(d => d.StateCreditCard,
                    o => o.MapFrom(s => s.StateCreditCard == CreditCardStatus.Activa ? "Activa" : "Cancelada"));

            CreateMap<CreditCardDto, CancelCreditCardViewModel>()
                .ForMember(d => d.CreditCardId, o => o.MapFrom(s => s.Id));

            CreateMap<CardConsumptionDto, CardConsumptionViewModel>()
                .ForMember(d => d.StateConsumption,
                    o => o.MapFrom(s => s.StateConsumption == ConsumptionStatus.Aprobado ? "APROBADO" : "RECHAZADO"));
        }
    }
}
