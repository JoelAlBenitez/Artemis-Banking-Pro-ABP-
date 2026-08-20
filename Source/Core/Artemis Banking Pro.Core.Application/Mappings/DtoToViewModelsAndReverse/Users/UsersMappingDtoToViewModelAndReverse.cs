using Artemis_Banking_Pro.Core.Application.ViewModels.Users;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.Users
{
    public sealed class UsersMappingDtoToViewModelAndReverse : Profile
    {
        public UsersMappingDtoToViewModelAndReverse()
        {
            CreateMap<UserDto, UserViewModel>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.IdUser))
                .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.Name} {s.LastName}"))
                .ForMember(d => d.State, o => o.MapFrom(s => s.State ? "Activo" : "Inactivo"))
                .ForMember(d => d.IsActive, o => o.MapFrom(s => s.State))
                .ForMember(d => d.TypeUser, o => o.MapFrom(s => s.TypeUser.ToString()));

            //La contraseña nunca se precarga y el monto adicional se pide en blanco
            CreateMap<UserDetailDto, EditUserViewModel>()
                .ForMember(d => d.NewPassword, o => o.Ignore())
                .ForMember(d => d.ConfirmNewPassword, o => o.Ignore())
                .ForMember(d => d.AdditionalAmount, o => o.Ignore());

            CreateMap<EditUserViewModel, EditUserDto>();

            //El origen del enlace de activación lo resuelve el controlador con la petición
            CreateMap<SaveUserViewModel, RegisterRequest>()
                .ForMember(d => d.Origin, o => o.Ignore());
        }
    }
}
