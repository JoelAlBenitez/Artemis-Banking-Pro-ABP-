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
                .ReverseMap();
        }
    }
}
