using AutoMapper;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;

namespace ArtemisBankingPro.Infraestructrue.Identity.Mappings
{
    public class IdentityProfile : Profile
    {
        public IdentityProfile()
        {
            CreateMap<ApplicationUser, UserDto>()
                .ForMember(dest => dest.IdUser, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.TypeUser, opt => opt.Ignore());

            CreateMap<ApplicationUser, ClientBaseDataDto>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName));

            CreateMap<ApplicationUser, ClientSummaryDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));

            CreateMap<ApplicationUser, UserDetailDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.IsClient, opt => opt.Ignore());

            CreateMap<ApplicationUser, EditUserDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.NewPassword, opt => opt.Ignore())
                .ForMember(dest => dest.ConfirmNewPassword, opt => opt.Ignore())
                .ForMember(dest => dest.AdditionalAmount, opt => opt.Ignore());
        }
    }
}


