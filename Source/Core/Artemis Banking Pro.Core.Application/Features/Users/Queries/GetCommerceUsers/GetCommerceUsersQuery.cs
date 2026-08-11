using Artemis_Banking_Pro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using AutoMapper;
using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Users.Queries.GetCommerceUsers
{
    /// <summary>
    /// Paginación del listado de usuarios con rol Comercio.
    /// </summary>
    /// <remarks>No lleva filtro por rol: siempre retorna únicamente usuarios Comercio.</remarks>
    public class GetCommerceUsersQuery : IRequest<PagedApiResponse<CommerceUserListItemDto>>
    {
        /// <example>1</example>
        [SwaggerParameter(Description = "Número de página que se desea consultar")]
        public int Page { get; set; } = 1;

        /// <example>20</example>
        [SwaggerParameter(Description = "Cantidad de registros por página. Máximo 20")]
        public int PageSize { get; set; } = DomainConstants.DefaultPageSize;
    }

    public class GetCommerceUsersQueryValidator : AbstractValidator<GetCommerceUsersQuery>
    {
        public GetCommerceUsersQueryValidator()
        {
            RuleFor(query => query.Page).ValidPage();
            RuleFor(query => query.PageSize).ValidPageSize();
        }
    }

    public class GetCommerceUsersQueryHandler
        : IRequestHandler<GetCommerceUsersQuery, PagedApiResponse<CommerceUserListItemDto>>
    {
        private readonly IUserManagementService _userManagementService;
        private readonly ICommerceRepository _commerceRepository;
        private readonly IMapper _mapper;

        public GetCommerceUsersQueryHandler(
            IUserManagementService userManagementService,
            ICommerceRepository commerceRepository,
            IMapper mapper)
        {
            _userManagementService = userManagementService;
            _commerceRepository = commerceRepository;
            _mapper = mapper;
        }

        public async Task<PagedApiResponse<CommerceUserListItemDto>> Handle(
            GetCommerceUsersQuery query, CancellationToken cancellationToken)
        {
            var users = await _userManagementService.GetCommerceUsersAsync(query.Page, query.PageSize);

            var items = users.Items.Select(_mapper.Map<CommerceUserListItemDto>).ToList();

            //Una sola consulta para toda la página en vez de una por usuario
            var userIds = items.Select(item => item.Id).ToList();
            var commerces = await _commerceRepository.GetAllFindAsync(
                commerce => commerce.AssociatedUserId != null && userIds.Contains(commerce.AssociatedUserId));

            foreach (var item in items)
            {
                var commerce = commerces.FirstOrDefault(entity => entity.AssociatedUserId == item.Id);
                if (commerce is null) continue;

                item.CommerceId = commerce.Id;
                item.CommerceName = commerce.Name;
            }

            return new PagedApiResponse<CommerceUserListItemDto>
            {
                Page = users.Page,
                PageSize = users.PageSize,
                TotalRecords = users.TotalCount,
                Data = items
            };
        }
    }
}
