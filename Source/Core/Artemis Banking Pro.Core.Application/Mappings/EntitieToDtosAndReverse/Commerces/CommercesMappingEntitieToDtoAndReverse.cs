using Artemis_Banking_Pro.Core.Application.DTOs.Commerces;
using AutoMapper;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;

namespace Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Commerces
{
    public sealed class CommercesMappingEntitieToDtoAndReverse : Profile
    {
        public CommercesMappingEntitieToDtoAndReverse()
        {
            CreateMap<Commerce, CommerceListItemDto>()
                .ForMember(dto => dto.IsActive,
                    opt => opt.MapFrom(commerce => commerce.Status == CommerceStatus.Activo))
                .ForMember(dto => dto.HasAssociatedUser,
                    opt => opt.MapFrom(commerce => commerce.AssociatedUserId != null));

            CreateMap<Commerce, CommerceDetailDto>()
                .ForMember(dto => dto.IsActive,
                    opt => opt.MapFrom(commerce => commerce.Status == CommerceStatus.Activo))
                //El usuario asociado vive en Identity: lo resuelve el handler
                .ForMember(dto => dto.AssociatedUser, opt => opt.Ignore());
        }
    }
}
