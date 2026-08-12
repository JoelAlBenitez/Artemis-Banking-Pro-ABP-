using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.DTOs.Commerces;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using AutoMapper;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Commerces.Queries.GetAllCommerces
{
    /// <summary>
    /// Filtros del listado paginado de comercios.
    /// </summary>
    public class GetAllCommercesQuery : IRequest<PagedApiResponse<CommerceListItemDto>>
    {
        /// <example>1</example>
        [SwaggerParameter(Description = "Número de página que se desea consultar")]
        public int Page { get; set; } = 1;

        /// <example>20</example>
        [SwaggerParameter(Description = "Cantidad de registros por página. Máximo 20")]
        public int PageSize { get; set; } = DomainConstants.DefaultPageSize;

        /// <example>activo</example>
        [SwaggerParameter(Description = "Estado de los comercios a consultar: activo, inactivo o todos")]
        public string? Status { get; set; }
    }

    public class GetAllCommercesQueryHandler
        : IRequestHandler<GetAllCommercesQuery, PagedApiResponse<CommerceListItemDto>>
    {
        private readonly ICommerceRepository _commerceRepository;
        private readonly IMapper _mapper;

        public GetAllCommercesQueryHandler(ICommerceRepository commerceRepository, IMapper mapper)
        {
            _commerceRepository = commerceRepository;
            _mapper = mapper;
        }

        public async Task<PagedApiResponse<CommerceListItemDto>> Handle(
            GetAllCommercesQuery query, CancellationToken cancellationToken)
        {
            var page = await _commerceRepository.GetPagedCommercesAsync(
                query.Page,
                query.PageSize,
                ApiFilterValues.Commerce.ToStatus(query.Status));

            return PagedApiResponse<CommerceListItemDto>.From(
                page, commerce => _mapper.Map<CommerceListItemDto>(commerce));
        }
    }
}
