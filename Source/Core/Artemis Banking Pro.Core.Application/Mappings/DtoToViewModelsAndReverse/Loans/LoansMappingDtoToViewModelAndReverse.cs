using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using Artemis_Banking_Pro.Core.Application.ViewModels.Loans;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.Loans
{
    public sealed class LoansMappingDtoToViewModelAndReverse : Profile
    {
        public LoansMappingDtoToViewModelAndReverse()
        {
            CreateMap<ClientLoansDto, ClientLoansViewModel>().ReverseMap();
            CreateMap<ConsultClientByIdCardDto, ConsultClientByIdCardViewModel>().ReverseMap();
            CreateMap<DetailLoansDto, DetailsLoansViewModel>().ReverseMap();
            CreateMap<EditAnnualInterestRateDto, EditAnnualInterestRateViewModel>().ReverseMap();
            CreateMap<LoansAssignmentDto, LoansAssigmentViewModel>().ReverseMap();
            CreateMap<LoansDto, LoansViewModel>().ReverseMap();
            CreateMap<LoansInstallmentDto, LoansAssigmentViewModel>().ReverseMap();
        }
    }
}
