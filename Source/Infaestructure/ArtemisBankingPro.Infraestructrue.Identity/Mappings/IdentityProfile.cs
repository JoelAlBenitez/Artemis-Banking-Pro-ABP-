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
                .ForMember(dest => dest.Role, opt => opt.Ignore());

            CreateMap<ApplicationUser, ClientBaseDataDto>();
        }
    }
}


