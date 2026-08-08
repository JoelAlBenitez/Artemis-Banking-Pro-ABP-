using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using AutoMapper;

namespace ArtemisBankingPro.Infraestructrue.Identity.Mappings
{
    //ApplicationUser no sale nunca de Identity: todo lo que consumen los demás módulos
    //viaja como DTO de la capa de aplicación.
    public class IdentityProfile : Profile
    {
        public IdentityProfile()
        {
            CreateMap<ApplicationUser, UserDto>()
                .ForMember(dest => dest.IdUser, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.IsActive))
                //El rol se resuelve con el UserManager, no está en la entidad
                .ForMember(dest => dest.TypeUser, opt => opt.Ignore());

            CreateMap<ApplicationUser, ClientBaseDataDto>();

            CreateMap<ApplicationUser, ClientSummaryDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));

            CreateMap<ApplicationUser, UserDetailDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.TypeUser, opt => opt.Ignore())
                .ForMember(dest => dest.IsClient, opt => opt.Ignore());

            //Carga del formulario de edición: la contraseña y el monto adicional siempre
            //llegan vacíos, nunca se precargan
            CreateMap<ApplicationUser, EditUserDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.NewPassword, opt => opt.Ignore())
                .ForMember(dest => dest.ConfirmNewPassword, opt => opt.Ignore())
                .ForMember(dest => dest.AdditionalAmount, opt => opt.Ignore());
        }
    }
}
