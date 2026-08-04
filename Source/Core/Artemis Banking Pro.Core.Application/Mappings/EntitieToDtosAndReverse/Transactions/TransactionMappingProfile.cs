using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.EntitieToDtosAndReverse.Transactions
{
    public sealed class TransactionMappingProfile : Profile
    {
        public TransactionMappingProfile()
        {
            CreateMap<Transaction, TransactionResultDto>()
                .ForMember(d => d.EffectiveAmount, o => o.MapFrom(s => s.Amount))
                .ReverseMap();
        }
    }
}
