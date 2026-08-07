using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using Artemis_Banking_Pro.Core.Application.ViewModels.Transactions;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Mappings.DtoToViewModelsAndReverse.Transactions
{
    public sealed class TransactionDtoToViewModelProfile : Profile
    {
        public TransactionDtoToViewModelProfile()
        {
            CreateMap<ExpressTransactionViewModel, ExpressTransactionDto>()
                .ReverseMap();

            CreateMap<PayCardViewModel, PayCreditCardDto>()
                .ReverseMap();

            CreateMap<PayLoanViewModel, PayLoanDto>()
                .ReverseMap();

            CreateMap<BeneficiaryTransactionViewModel, BeneficiaryTransactionDto>()
                .ReverseMap();

            CreateMap<CashAdvanceViewModel, CashAdvanceRequestDto>()
                .ReverseMap();

            CreateMap<AccountTransferViewModel, AccountTransferDto>()
                .ReverseMap();
        }
    }
}
