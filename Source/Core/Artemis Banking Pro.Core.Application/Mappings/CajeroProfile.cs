using ArtemisBankingPro.Core.Application.DTOs.Cajero;
using ArtemisBankingPro.Core.Application.ViewModels.Cajero;
using AutoMapper;

namespace ArtemisBankingPro.Core.Application.Mappings
{
    public class CajeroProfile : Profile
    {
        public CajeroProfile()
        {
            CreateMap<ConfirmacionRetiroViewModel, TransaccionDto>()
                .ForMember(dest => dest.TipoTransaccion, opt => opt.MapFrom(src => "DÉBITO"))
                .ForMember(dest => dest.Origen, opt => opt.MapFrom(src => src.NumeroCuentaOrigen))
                .ForMember(dest => dest.Beneficiario, opt => opt.MapFrom(src => "RETIRO"))
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => "APROBADA"))
                .ForMember(dest => dest.Fecha, opt => opt.Ignore())
                .ForMember(dest => dest.UsuarioResponsable, opt => opt.Ignore());
        }
    }
}
