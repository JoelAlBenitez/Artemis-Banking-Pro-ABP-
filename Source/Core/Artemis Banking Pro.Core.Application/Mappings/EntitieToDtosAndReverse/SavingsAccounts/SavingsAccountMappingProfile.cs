using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.SavingsAccounts
{
    public sealed class SavingsAccountMappingProfile : Profile
    {
        public SavingsAccountMappingProfile()
        {
            CreateMap<SavingsAccount, SavingsAccountDto>()
                .ForMember(d => d.TypeSavingsAccount, o => o.MapFrom(s => s.AccountType))
                .ForMember(d => d.StateSavingsAccount, o => o.MapFrom(s => s.Status))
                .ForMember(d => d.FullNameCustomer, o => o.Ignore())
                .ForMember(d => d.IdCard, o => o.Ignore())
                .ReverseMap()
                .ForMember(d => d.AccountType, o => o.MapFrom(s => s.TypeSavingsAccount))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.StateSavingsAccount));
        }
    }
}
