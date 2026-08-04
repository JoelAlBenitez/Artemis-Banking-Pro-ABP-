using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.CreditCards
{
    public sealed class CreditCardsMappingEntitieToDtoAndReverse : Profile
    {
        public CreditCardsMappingEntitieToDtoAndReverse()
        {
            //CardNumber y CvcHash no se proyectan: no existen en el dto destino.
            //El nombre del cliente proviene de Identity y lo completa el servicio.
            CreateMap<CreditCard, CreditCardDto>()
                .ForMember(d => d.MaskedCardNumber,
                    o => o.MapFrom(s => $"**** **** **** {s.LastFourDigits}"))
                .ForMember(d => d.ExpirationDate,
                    o => o.MapFrom(s => s.ExpirationDate.ToString("MM/yy")))
                .ForMember(d => d.AvailableCredit,
                    o => o.MapFrom(s => s.CreditLimit - s.OwedAmount))
                .ForMember(d => d.StateCreditCard, o => o.MapFrom(s => s.Status))
                .ForMember(d => d.FullNameCustomer, o => o.Ignore());

            CreateMap<CreditCard, EditCardLimitDto>();

            CreateMap<CardConsumption, CardConsumptionDto>()
                .ForMember(d => d.ConsumptionDate, o => o.MapFrom(s => s.CreatedAt))
                .ForMember(d => d.StateConsumption, o => o.MapFrom(s => s.Status));

            //Campos de solo sistema: numero, ultimos digitos, cvc, expiracion, deuda, estado y auditoria
            CreateMap<CreditCardAssignmentDto, CreditCard>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.CardNumber, o => o.Ignore())
                .ForMember(d => d.LastFourDigits, o => o.Ignore())
                .ForMember(d => d.CvcHash, o => o.Ignore())
                .ForMember(d => d.ExpirationDate, o => o.Ignore())
                .ForMember(d => d.OwedAmount, o => o.MapFrom(_ => 0m))
                .ForMember(d => d.Status, o => o.MapFrom(_ => CreditCardStatus.Activa))
                .ForMember(d => d.AssignedByAdminId, o => o.Ignore())
                .ForMember(d => d.cardConsumptions, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.CreateByUserId, o => o.Ignore())
                .ForMember(d => d.LastModifiedByIdUser, o => o.Ignore())
                .ForMember(d => d.ModifiedAt, o => o.Ignore());
        }
    }
}
