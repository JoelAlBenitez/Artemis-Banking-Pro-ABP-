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
                .ForMember(d => d.Name, o => o.MapFrom(s => s.OwnerFullName.StartsWith("Cliente ") ? "Cliente" : s.OwnerFullName))
                .ForMember(d => d.LastName, o => o.MapFrom(s => s.OwnerFullName.StartsWith("Cliente ") ? s.OwnerFullName.Replace("Cliente ", "") : "Asociado"))
                .ReverseMap();
        }
    }
}
