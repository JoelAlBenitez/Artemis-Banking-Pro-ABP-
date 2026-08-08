<<<<<<< HEAD
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
=======
using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using Artemis_Banking_Pro.Core.Application.ViewModels.Transactions;
>>>>>>> origin/development
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
<<<<<<< HEAD
=======

            CreateMap<CashAdvance, CashAdvanceDto>()
                .ForMember(d => d.CardLastFourDigits, o => o.MapFrom(s => s.CreditCard != null ? s.CreditCard.LastFourDigits : ""))
                .ForMember(d => d.AccountLastFourDigits, o => o.MapFrom(s => s.SavingsAccount != null && s.SavingsAccount.AccountNumber.Length >= 4 
                    ? s.SavingsAccount.AccountNumber.Substring(s.SavingsAccount.AccountNumber.Length - 4) 
                    : ""));

            CreateMap<AccountTransferViewModel, AccountTransferDto>().ReverseMap();
            CreateMap<SavingsAccountDto, SavingsAccountSelectViewModel>().ReverseMap();
>>>>>>> origin/development
        }
    }
}
