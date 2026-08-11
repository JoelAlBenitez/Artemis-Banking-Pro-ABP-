using Artemis_Banking_Pro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using AutoMapper;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Users.Queries.GetAllUsers
{
    /// <summary>
    /// Filtros del listado paginado de usuarios, excluyendo siempre el rol Comercio.
    /// </summary>
    public class GetAllUsersQuery : IRequest<PagedApiResponse<UserListItemDto>>
    {
        /// <example>1</example>
        [SwaggerParameter(Description = "Número de página que se desea consultar")]
        public int Page { get; set; } = 1;

        /// <example>20</example>
        [SwaggerParameter(Description = "Cantidad de registros por página. Máximo 20")]
        public int PageSize { get; set; } = DomainConstants.DefaultPageSize;

        /// <example>Cliente</example>
        [SwaggerParameter(Description = "Filtra por tipo de usuario: administrador, cajero o cliente")]
        public string? Role { get; set; }
    }

    public class GetAllUsersQueryHandler
        : IRequestHandler<GetAllUsersQuery, PagedApiResponse<UserListItemDto>>
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IMapper _mapper;

        public GetAllUsersQueryHandler(IUserManagementService userManagementService, IMapper mapper)
        {
            _userManagementService = userManagementService;
            _mapper = mapper;
        }

        public async Task<PagedApiResponse<UserListItemDto>> Handle(
            GetAllUsersQuery query, CancellationToken cancellationToken)
        {
            var role = ApiFilterValues.User.ToRole(query.Role);

            //Ambos contratos de Identity excluyen el rol Comercio, que tiene su propio listado
            var users = role is null
                ? await _userManagementService.GetUsersAsync(query.Page, query.PageSize, StatusFilter.Todos)
                : await _userManagementService.GetUsersByRoleAsync(role.Value, query.Page, query.PageSize);

            return new PagedApiResponse<UserListItemDto>
            {
                Page = users.Page,
                PageSize = users.PageSize,
                TotalRecords = users.TotalCount,
                Data = users.Items.Select(_mapper.Map<UserListItemDto>).ToList()
            };
        }
    }
}
