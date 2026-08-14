using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.CreditCards
{
    public sealed class CreditCardsApiMapping : Profile
    {
        private const string Approved = "APROBADO";
        private const string Rejected = "RECHAZADO";

        public CreditCardsApiMapping()
        {
            //El enmascarado y los últimos cuatro dígitos ya vienen resueltos por el perfil del
            //módulo: aquí solo se renombra al contrato de la API. El número completo no viaja.
            CreateMap<CreditCardDto, CreditCardListItemDto>()
                .ForMember(dto => dto.ClientId, opt => opt.MapFrom(source => source.CustomerId))
                .ForMember(dto => dto.ClientFullName, opt => opt.MapFrom(source => source.FullNameCustomer))
                .ForMember(dto => dto.CurrentDebt, opt => opt.MapFrom(source => source.OwedAmount))
                .ForMember(dto => dto.Status, opt => opt.MapFrom(source => source.StateCreditCard.ToString()));

            CreateMap<CreditCardDto, CreditCardDetailDto>()
                .IncludeBase<CreditCardDto, CreditCardListItemDto>()
                .ForMember(dto => dto.Consumptions, opt => opt.Ignore());

            CreateMap<CardConsumptionDto, CardConsumptionApiDto>()
                .ForMember(dto => dto.Date, opt => opt.MapFrom(source => source.ConsumptionDate))
                .ForMember(dto => dto.Status,
                    opt => opt.MapFrom(source =>
                        source.StateConsumption == ConsumptionStatus.Aprobado ? Approved : Rejected));
        }
    }
}
