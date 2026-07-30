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
            CreateMap<ClientLoansDto, ClientLoansViewModel>().ReverseMap();
            CreateMap<ConsultClientByIdCardDto, ConsultClientByIdCardViewModel>().ReverseMap();
            CreateMap<LoansAssignmentDto, LoansAssigmentViewModel>().ReverseMap();
            CreateMap<LoansFilterDto, LoansFilterViewModel>().ReverseMap();

            CreateMap<EditAnnualInterestRateDto, EditAnnualInterestRateViewModel>()
                .ForMember(d => d.LoansId, o => o.MapFrom(s => s.Id))
                .ReverseMap()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.LoansId));

            CreateMap<LoansDto, LoansViewModel>()
                .ForMember(d => d.StateLoans,
                    o => o.MapFrom(s => s.StateLoans == LoanStatus.Activo ? "Activo" : "Completado"))
                .ForMember(d => d.StateCustomer,
                    o => o.MapFrom(s => s.CustomerInArrears ? "En mora" : "Al día"));

            CreateMap<DetailLoansDto, DetailsLoansViewModel>()
                .ForMember(d => d.StateLoans,
                    o => o.MapFrom(s => s.StateLoans == LoanStatus.Activo ? "Activo" : "Completado"))
                .ForMember(d => d.loasInstallmentViewModels,
                    o => o.MapFrom(s => s.loansInstallmentDtos));

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
