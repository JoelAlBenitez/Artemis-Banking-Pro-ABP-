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
                .ForMember(d => d.AccountNumber, o => o.MapFrom(s => s.BeneficiaryAccountNumber))
                .ForMember(d => d.OwnerFullName, o => o.MapFrom(s => s.BeneficiarySavingsAccount != null 
                    ? $"Cliente {s.BeneficiarySavingsAccount.CustomerId}" 
                    : "Cliente Desconocido"))
                .ReverseMap()
                .ForMember(d => d.BeneficiarySavingsAccount, o => o.Ignore());

            CreateMap<SaveBeneficiaryDto, Beneficiary>()
                .ForMember(d => d.BeneficiaryAccountNumber, o => o.MapFrom(s => s.AccountNumber))
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
