using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Domain.CodeErrors.LoansErros;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using AutoMapper;
using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Loans.Queries.GetAllLoans
{
    /// <summary>
    /// Filtros del listado paginado de préstamos.
    /// </summary>
    public class GetAllLoansQuery : IRequest<PagedApiResponse<LoanListItemDto>>
    {
        /// <example>1</example>
        [SwaggerParameter(Description = "Número de página que se desea consultar")]
        public int Page { get; set; } = 1;

        /// <example>20</example>
        [SwaggerParameter(Description = "Cantidad de registros por página. Máximo 20")]
        public int PageSize { get; set; } = DomainConstants.DefaultPageSize;

        /// <example>activos</example>
        [SwaggerParameter(Description = "Estado de los préstamos: activos, completados o todos")]
        public string? Status { get; set; }

        /// <example>00187654321</example>
        [SwaggerParameter(Description = "Cédula del cliente para buscar sus préstamos")]
        public string? Identification { get; set; }
    }

    public class GetAllLoansQueryValidator : AbstractValidator<GetAllLoansQuery>
    {
        public GetAllLoansQueryValidator()
        {
            RuleFor(query => query.Page).ValidPage();
            RuleFor(query => query.PageSize).ValidPageSize();

            RuleFor(query => query.Status)
                .Must(ApiFilterValues.Loan.IsAllowed)
                .WithMessage("El estado solo puede ser activos, completados o todos.");
        }
    }

    public class GetAllLoansQueryHandler
        : IRequestHandler<GetAllLoansQuery, PagedApiResponse<LoanListItemDto>>
    {
        //El servicio devuelve fallo cuando el filtro no encuentra nada porque la WebApp lo pinta
        //como mensaje en pantalla. El contrato del listado de la API solo admite 200/400/401/403,
        //así que aquí "sin coincidencias" es una página vacía, no un 404.
        private static readonly Error[] EmptyResultErrors =
        [
            LoansError.NonExistsCustomerByIdCard,
            LoansError.NonExistsLoans
        ];

        private readonly ILoansServices _loansServices;
        private readonly IMapper _mapper;

        public GetAllLoansQueryHandler(ILoansServices loansServices, IMapper mapper)
        {
            _loansServices = loansServices;
            _mapper = mapper;
        }

        public async Task<PagedApiResponse<LoanListItemDto>> Handle(
            GetAllLoansQuery query, CancellationToken cancellationToken)
        {
            var result = await _loansServices.GetPagedLoansAsync(new LoansFilterDto
            {
                IdCard = query.Identification,
                Status = ApiFilterValues.Loan.ToStatusFilter(query.Status),
                Page = query.Page,
                PageSize = query.PageSize
            });

            if (result.Errors.Any(EmptyResultErrors.Contains))
                return PagedApiResponse<LoanListItemDto>.Empty(query.Page, query.PageSize);

            var page = ValidationResultGuard.EnsureSuccess(result);

            return PagedApiResponse<LoanListItemDto>.From(
                page, loan => _mapper.Map<LoanListItemDto>(loan));
        }
    }
}
