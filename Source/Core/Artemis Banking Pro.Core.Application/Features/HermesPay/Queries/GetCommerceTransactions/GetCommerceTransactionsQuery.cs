using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Contracts.Commerces;
using Artemis_Banking_Pro.Core.Application.DTOs.Commerces;
using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Domain.CodeErrors.CommercesErrors;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.HermesPay.Queries.GetCommerceTransactions
{
    /// <summary>
    /// Transacciones recibidas por un comercio.
    /// </summary>
    public class GetCommerceTransactionsQuery : IRequest<CommercePaymentsPageDto>
    {
        [SwaggerParameter(Description = "Identificador del comercio. Se ignora si el usuario autenticado tiene rol Comercio")]
        public int CommerceId { get; set; }

        /// <example>1</example>
        [SwaggerParameter(Description = "Número de página que se desea consultar")]
        public int Page { get; set; } = 1;

        /// <example>20</example>
        [SwaggerParameter(Description = "Cantidad de registros por página. Máximo 20")]
        public int PageSize { get; set; } = DomainConstants.DefaultPageSize;
    }

    public class GetCommerceTransactionsQueryValidator : AbstractValidator<GetCommerceTransactionsQuery>
    {
        public GetCommerceTransactionsQueryValidator()
        {
            RuleFor(query => query.Page).ValidPage();
            RuleFor(query => query.PageSize).ValidPageSize();
        }
    }

    public class GetCommerceTransactionsQueryHandler
        : IRequestHandler<GetCommerceTransactionsQuery, CommercePaymentsPageDto>
    {
        private const string Approved = "APROBADO";
        private const string Rejected = "RECHAZADO";

        private static readonly Error[] NotFoundErrors = [CommerceError.NonExistsCommerce];

        private readonly ICommerceAccessService _commerceAccessService;
        private readonly ICommercePaymentRepository _commercePaymentRepository;

        public GetCommerceTransactionsQueryHandler(
            ICommerceAccessService commerceAccessService,
            ICommercePaymentRepository commercePaymentRepository)
        {
            _commerceAccessService = commerceAccessService;
            _commercePaymentRepository = commercePaymentRepository;
        }

        public async Task<CommercePaymentsPageDto> Handle(
            GetCommerceTransactionsQuery query, CancellationToken cancellationToken)
        {
            var access = await _commerceAccessService.ResolveCommerceAsync(query.CommerceId);

            //Un usuario Comercio sin comercio asociado no puede operar: el documento lo
            //responde como acceso denegado, no como recurso inexistente.
            if (!access.IsValid && access.Errors.Contains(CommerceError.NonExistsCommerce)
                && query.CommerceId <= 0)
                throw new ForbiddenException(CommerceError.CommerceWithoutAssociatedUser);

            var commerce = ValidationResultGuard.EnsureSuccess(access, NotFoundErrors);

            var payments = await _commercePaymentRepository.GetPagedPaymentsByCommerceAsync(
                commerce.Id, query.Page, query.PageSize);

            var page = PagedApiResponse<CommercePaymentDto>.From(payments, payment => new CommercePaymentDto
            {
                Id = payment.Id,
                TransactionDate = payment.CreatedAt,
                Amount = payment.Amount,
                CardLastFourDigits = payment.CardLastFourDigits,
                Status = payment.Status == ConsumptionStatus.Aprobado ? Approved : Rejected
            });

            return CommercePaymentsPageDto.From(page, commerce.Id, commerce.Name);
        }
    }
}
