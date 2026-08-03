using Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.ViewModels.Beneficiaries;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.Beneficiaries
{
    public sealed class BeneficiaryDtoToViewModelProfile : Profile
    {
        public BeneficiaryDtoToViewModelProfile()
        {
            CreateMap<SaveBeneficiaryViewModel, SaveBeneficiaryDto>()
                .ForMember(d => d.OwnerClientId, o => o.Ignore())
                .ReverseMap();

            CreateMap<BeneficiaryDto, BeneficiaryListViewModel>()
                .ForMember(d => d.Name, o => o.Ignore())
                .ForMember(d => d.LastName, o => o.Ignore())
                .ReverseMap();
        }
    }
}
