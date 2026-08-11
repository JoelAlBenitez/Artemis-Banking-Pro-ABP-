using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Domain.CodeErrors.SavingsAccountsErrors;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using AutoMapper;
using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.SavingsAccounts.Queries.GetAllSavingsAccounts
{
    /// <summary>
    /// Filtros del listado paginado de cuentas de ahorro.
    /// </summary>
    public class GetAllSavingsAccountsQuery : IRequest<PagedApiResponse<SavingsAccountListItemDto>>
    {
        /// <example>1</example>
        [SwaggerParameter(Description = "Número de página que se desea consultar")]
        public int Page { get; set; } = 1;

        /// <example>20</example>
        [SwaggerParameter(Description = "Cantidad de registros por página. Máximo 20")]
        public int PageSize { get; set; } = DomainConstants.DefaultPageSize;

        /// <example>00187654321</example>
        [SwaggerParameter(Description = "Cédula del cliente para buscar sus cuentas de ahorro")]
        public string? Identification { get; set; }

        /// <example>activa</example>
        [SwaggerParameter(Description = "Estado de las cuentas: activa, cancelada o todas")]
        public string? Status { get; set; }

        /// <example>todas</example>
        [SwaggerParameter(Description = "Tipo de cuenta: principal, secundaria o todas")]
        public string? Type { get; set; }
    }

    public class GetAllSavingsAccountsQueryValidator : AbstractValidator<GetAllSavingsAccountsQuery>
    {
        public GetAllSavingsAccountsQueryValidator()
        {
            RuleFor(query => query.Page).ValidPage();
            RuleFor(query => query.PageSize).ValidPageSize();

            RuleFor(query => query.Status)
                .Must(ApiFilterValues.SavingsAccount.IsAllowedStatus)
                .WithMessage("El estado solo puede ser activa, cancelada o todas.");

            RuleFor(query => query.Type)
                .Must(ApiFilterValues.SavingsAccount.IsAllowedType)
                .WithMessage("El tipo de cuenta solo puede ser principal, secundaria o todas.");
        }
    }

    public class GetAllSavingsAccountsQueryHandler
        : IRequestHandler<GetAllSavingsAccountsQuery, PagedApiResponse<SavingsAccountListItemDto>>
    {
        //Buscar por una cédula sin cliente o sin cuentas es un recurso inexistente, no un
        //error de datos: el documento lo responde como 404.
        private static readonly Error[] NotFoundErrors =
        [
            SavingsAccountError.NonExistsCustomerByIdCard,
            SavingsAccountError.NonExistsSavingsAccounts
        ];

        private readonly ISavingsAccountsServices _savingsAccountsServices;
        private readonly IMapper _mapper;

        public GetAllSavingsAccountsQueryHandler(
            ISavingsAccountsServices savingsAccountsServices, IMapper mapper)
        {
            _savingsAccountsServices = savingsAccountsServices;
            _mapper = mapper;
        }

        public async Task<PagedApiResponse<SavingsAccountListItemDto>> Handle(
            GetAllSavingsAccountsQuery query, CancellationToken cancellationToken)
        {
            var result = await _savingsAccountsServices.GetPagedSavingsAccountsAsync(new SavingsAccountFilterDto
            {
                IdCard = query.Identification,
                Status = ApiFilterValues.SavingsAccount.ToStatusFilter(query.Status),
                Type = ApiFilterValues.SavingsAccount.ToTypeFilter(query.Type),
                Page = query.Page,
                PageSize = query.PageSize
            });

            var page = ValidationResultGuard.EnsureSuccess(result, NotFoundErrors);

            return PagedApiResponse<SavingsAccountListItemDto>.From(
                page, account => _mapper.Map<SavingsAccountListItemDto>(account));
        }
    }
}
