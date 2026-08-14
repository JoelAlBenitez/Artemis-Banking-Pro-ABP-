using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using Artemis_Banking_Pro.Core.Application.ViewModels.Loans;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.Loans
{
    public sealed class LoansMappingDtoToViewModelAndReverse : Profile
    {
        public LoansMappingDtoToViewModelAndReverse()
        {

            //write loans. FullNameCustomer solo existe en el ViewModel: el controlador lo
            //completa con el cliente elegido en el paso 1 para mostrarlo en el formulario.
            CreateMap<LoansAssignmentDto, LoansAssigmentViewModel>()
                .ForMember(d => d.FullNameCustomer, o => o.Ignore())
                .ReverseMap();

            CreateMap<LoansFilterDto, LoansFilterViewModel>().ReverseMap();
            
            CreateMap<EditAnnualInterestRateDto, EditAnnualInterestRateViewModel>()
                .ForMember(d => d.LoansId, o => o.MapFrom(s => s.Id))
                .ReverseMap()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.LoansId));

            CreateMap<ClientLoansDto, ClientLoansViewModel>();


            //paso 1 de la asignacion: promedio de deuda y clientes elegibles
            CreateMap<ClientsForLoanAssignmentDto, ClientsForLoanAssignmentViewModel>()
                .ForMember(d => d.IdCard, o => o.Ignore());

            //pantalla de advertencia de riesgo: los datos del prestamo y los del riesgo se
            //proyectan por separado sobre el mismo modelo
            CreateMap<LoansAssignmentDto, RiskWarningViewModel>()
                .ForMember(d => d.Message, o => o.Ignore())
                .ForMember(d => d.CurrentDebt, o => o.Ignore())
                .ForMember(d => d.ProjectedDebt, o => o.Ignore())
                .ForMember(d => d.AverageDebt, o => o.Ignore());

            CreateMap<LoanRiskEvaluationDto, RiskWarningViewModel>()
                .ForMember(d => d.CustomerId, o => o.Ignore())
                .ForMember(d => d.TermLoans, o => o.Ignore())
                .ForMember(d => d.AmmountLoans, o => o.Ignore())
                .ForMember(d => d.AnnualInterestRate, o => o.Ignore());

            CreateMap<RiskWarningViewModel, LoansAssignmentDto>()
                .ForMember(d => d.ConfirmHighRisk, o => o.Ignore());
            //ventana principal de prestamo
            CreateMap<LoansDto, LoansViewModel>()
                .ForMember(d => d.StateLoans,
                    o => o.MapFrom(s => s.StateLoans == LoanStatus.Activo ? "Activo" : "Completado"))
                .ForMember(d => d.StateCustomer,
                    o => o.MapFrom(s => s.CustomerInArrears ? "En mora" : "Al día"));

            //detalles del prestamo
            CreateMap<DetailLoansDto, DetailsLoansViewModel>()
                .ForMember(d => d.StateLoans,
                    o => o.MapFrom(s => s.StateLoans == LoanStatus.Activo ? "Activo" : "Completado"))
                .ForMember(d => d.loasInstallmentViewModels,
                    o => o.MapFrom(s => s.loansInstallmentDtos));

            //cuotas de cada prestamo -> visualizada en los detalles 
            CreateMap<LoansInstallmentDto, LoasInstallmentViewModel>()
                .ForMember(d => d.StateInstallment,
                    o => o.MapFrom(s => s.StateInstallment == PaymentStatus.Pendiente
                        ? "Pendiente"
                        : s.StateInstallment == PaymentStatus.ParcialmentePagada
                            ? "Parcialmente pagada"
                            : "Pagada"));
        }
    }
}
