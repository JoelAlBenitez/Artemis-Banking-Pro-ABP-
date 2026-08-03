using Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Entities.Beneficiaries;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Beneficiaries
{
    public sealed class BeneficiaryMappingProfile : Profile
    {
        public BeneficiaryMappingProfile()
        {
            CreateMap<Beneficiary, BeneficiaryDto>()
                .ForMember(d => d.OwnerFullName, o => o.Ignore())
                .ReverseMap();

            CreateMap<SaveBeneficiaryDto, Beneficiary>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.IsActive, o => o.Ignore())
                .ForMember(d => d.DeactivatedAt, o => o.Ignore())
                .ForMember(d => d.BeneficiarySavingsAccountId, o => o.Ignore())
                .ForMember(d => d.BeneficiarySavingsAccount, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.CreateByUserId, o => o.Ignore())
                .ForMember(d => d.LastModifiedByIdUser, o => o.Ignore())
                .ForMember(d => d.ModifiedAt, o => o.Ignore());
        }
    }
}
