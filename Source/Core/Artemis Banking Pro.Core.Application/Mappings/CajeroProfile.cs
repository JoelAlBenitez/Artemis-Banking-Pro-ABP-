using ArtemisBankingPro.Core.Application.DTOs.Cajero;
using ArtemisBankingPro.Core.Application.ViewModels.Cajero;
using AutoMapper;

namespace ArtemisBankingPro.Core.Application.Mappings
{
    public class CajeroProfile : Profile
    {
        public CajeroProfile()
        {
            CreateMap<ConfirmacionDepositoViewModel, TransaccionDto>()
                .ForMember(dest => dest.TipoTransaccion, opt => opt.MapFrom(src => "CRÉDITO"))
                .ForMember(dest => dest.Origen, opt => opt.MapFrom(src => "DEPÓSITO"))
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => "APROBADA"))
                .ForMember(dest => dest.Beneficiario, opt => opt.MapFrom(src => src.NumeroCuentaDestino))
                .ForMember(dest => dest.Fecha, opt => opt.Ignore())
                .ForMember(dest => dest.UsuarioResponsable, opt => opt.Ignore());
        }
    }
}
