using FluentValidation;
using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using ArtemisBankingPro.Core.Domain.CodeErrors.CreditCardsErrors;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using AutoMapper;
using MediatR;

namespace Artemis_Banking_Pro.Core.Application.Features.CreditCards.Queries.GetCreditCardById
{
    public class GetCreditCardByIdQuery : IRequest<CreditCardDetailDto>
    {
        public required int Id { get; set; }

        public int Page { get; set; } = 1;
    }

    public class GetCreditCardByIdQueryValidator : AbstractValidator<GetCreditCardByIdQuery>
    {
        public GetCreditCardByIdQueryValidator()
        {
            RuleFor(query => query.Id).ValidIdentifier();
            RuleFor(query => query.Page).ValidPage();
        }
    }

    public class GetCreditCardByIdQueryHandler
        : IRequestHandler<GetCreditCardByIdQuery, CreditCardDetailDto>
    {
        private static readonly Error[] NotFoundErrors = [CreditCardError.NonExistsCreditCard];

        private readonly ICreditCardsServices _creditCardsServices;
        private readonly IMapper _mapper;

        public GetCreditCardByIdQueryHandler(ICreditCardsServices creditCardsServices, IMapper mapper)
        {
            _creditCardsServices = creditCardsServices;
            _mapper = mapper;
        }

        public async Task<CreditCardDetailDto> Handle(
            GetCreditCardByIdQuery query, CancellationToken cancellationToken)
        {
            var cardResult = await _creditCardsServices.GetByIdAsync(query.Id);
            var card = ValidationResultGuard.EnsureSuccess(cardResult, NotFoundErrors);

            var detail = _mapper.Map<CreditCardDetailDto>(card);

            //Aprobados y rechazados, del más reciente al más antiguo. Los avances de efectivo
            //muestran el literal AVANCE en lugar de un nombre de comercio.
            var consumptionsResult = await _creditCardsServices.GetPagedConsumptionsAsync(
                query.Id, query.Page, DomainConstants.MaxPageSize);

            var consumptions = ValidationResultGuard.EnsureSuccess(consumptionsResult, NotFoundErrors);

            detail.Consumptions = consumptions.Items
                .Select(_mapper.Map<CardConsumptionApiDto>)
                .ToList();

            return detail;
        }
    }
}
