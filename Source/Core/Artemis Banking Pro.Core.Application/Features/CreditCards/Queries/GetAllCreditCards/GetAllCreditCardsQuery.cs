using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Domain.CodeErrors.CreditCardsErrors;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using AutoMapper;
using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.CreditCards.Queries.GetAllCreditCards
{
    /// <summary>
    /// Filtros del listado paginado de tarjetas de crédito.
    /// </summary>
    public class GetAllCreditCardsQuery : IRequest<PagedApiResponse<CreditCardListItemDto>>
    {
        /// <example>1</example>
        [SwaggerParameter(Description = "Número de página que se desea consultar")]
        public int Page { get; set; } = 1;

        /// <example>20</example>
        [SwaggerParameter(Description = "Cantidad de registros por página. Máximo 20")]
        public int PageSize { get; set; } = DomainConstants.DefaultPageSize;

        /// <example>activa</example>
        [SwaggerParameter(Description = "Estado de las tarjetas: activa, cancelada o todas")]
        public string? Status { get; set; }

        /// <example>00187654321</example>
        [SwaggerParameter(Description = "Cédula del cliente para buscar sus tarjetas")]
        public string? Identification { get; set; }
    }

    public class GetAllCreditCardsQueryValidator : AbstractValidator<GetAllCreditCardsQuery>
    {
        public GetAllCreditCardsQueryValidator()
        {
            RuleFor(query => query.Page).ValidPage();
            RuleFor(query => query.PageSize).ValidPageSize();

            RuleFor(query => query.Status)
                .Must(ApiFilterValues.CreditCard.IsAllowed)
                .WithMessage("El estado solo puede ser activa, cancelada o todas.");
        }
    }

    public class GetAllCreditCardsQueryHandler
        : IRequestHandler<GetAllCreditCardsQuery, PagedApiResponse<CreditCardListItemDto>>
    {
        //El servicio devuelve fallo cuando el filtro no encuentra nada porque la WebApp lo pinta
        //como mensaje en pantalla. El contrato del listado de la API solo admite 200/400/401/403,
        //así que aquí "sin coincidencias" es una página vacía, no un 404.
        private static readonly Error[] EmptyResultErrors =
        [
            CreditCardError.NonExistsCustomerByIdCard,
            CreditCardError.NonExistsCreditCards
        ];

        private readonly ICreditCardsServices _creditCardsServices;
        private readonly IMapper _mapper;

        public GetAllCreditCardsQueryHandler(ICreditCardsServices creditCardsServices, IMapper mapper)
        {
            _creditCardsServices = creditCardsServices;
            _mapper = mapper;
        }

        public async Task<PagedApiResponse<CreditCardListItemDto>> Handle(
            GetAllCreditCardsQuery query, CancellationToken cancellationToken)
        {
            var result = await _creditCardsServices.GetPagedCreditCardsAsync(new CreditCardFilterDto
            {
                IdCard = query.Identification,
                Status = ApiFilterValues.CreditCard.ToStatusFilter(query.Status),
                Page = query.Page,
                PageSize = query.PageSize
            });

            if (result.Errors.Any(EmptyResultErrors.Contains))
                return PagedApiResponse<CreditCardListItemDto>.Empty(query.Page, query.PageSize);

            var page = ValidationResultGuard.EnsureSuccess(result);

            return PagedApiResponse<CreditCardListItemDto>.From(
                page, card => _mapper.Map<CreditCardListItemDto>(card));
        }
    }
}
