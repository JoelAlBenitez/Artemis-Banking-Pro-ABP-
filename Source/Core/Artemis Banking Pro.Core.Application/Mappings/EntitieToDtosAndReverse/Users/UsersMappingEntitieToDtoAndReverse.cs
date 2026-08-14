using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Users
{
    //Traduce los contratos de Identity a la forma que el documento funcional exige en la API.
    public sealed class UsersMappingEntitieToDtoAndReverse : Profile
    {
        public UsersMappingEntitieToDtoAndReverse()
        {
            CreateMap<UserDto, UserListItemDto>()
                .ForMember(dto => dto.Id, opt => opt.MapFrom(user => user.IdUser))
                .ForMember(dto => dto.Identification, opt => opt.MapFrom(user => user.IDCARD))
                .ForMember(dto => dto.FirstName, opt => opt.MapFrom(user => user.Name))
                .ForMember(dto => dto.Role, opt => opt.MapFrom(user => user.TypeUser.ToString()))
                .ForMember(dto => dto.IsActive, opt => opt.MapFrom(user => user.State));

            CreateMap<UserDto, CommerceUserListItemDto>()
                .IncludeBase<UserDto, UserListItemDto>()
                //El comercio vive en Persistence: lo resuelve el handler
                .ForMember(dto => dto.CommerceId, opt => opt.Ignore())
                .ForMember(dto => dto.CommerceName, opt => opt.Ignore());

            CreateMap<UserDetailDto, UserApiDetailDto>()
                .ForMember(dto => dto.Identification, opt => opt.MapFrom(user => user.IDCARD))
                .ForMember(dto => dto.FirstName, opt => opt.MapFrom(user => user.Name))
                .ForMember(dto => dto.Role, opt => opt.MapFrom(user => user.TypeUser.ToString()))
                .ForMember(dto => dto.IsActive, opt => opt.MapFrom(user => user.State))
                .ForMember(dto => dto.MainAccount, opt => opt.Ignore());

            CreateMap<SavingsAccount, MainAccountDto>()
                .ForMember(dto => dto.IsPrincipal, opt => opt.MapFrom(account => account.IsPrimary))
                .ForMember(dto => dto.Status, opt => opt.MapFrom(account => account.Status.ToString()));
        }
    }
}
