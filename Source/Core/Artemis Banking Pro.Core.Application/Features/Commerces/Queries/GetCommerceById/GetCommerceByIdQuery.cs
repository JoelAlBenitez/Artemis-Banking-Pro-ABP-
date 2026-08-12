using Artemis_Banking_Pro.Core.Application.DTOs.Commerces;
using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Domain.CodeErrors.CommercesErrors;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using AutoMapper;
using MediatR;

namespace Artemis_Banking_Pro.Core.Application.Features.Commerces.Queries.GetCommerceById
{
    public class GetCommerceByIdQuery : IRequest<CommerceDetailDto>
    {
        public required int Id { get; set; }
    }

    public class GetCommerceByIdQueryHandler : IRequestHandler<GetCommerceByIdQuery, CommerceDetailDto>
    {
        private readonly ICommerceRepository _commerceRepository;
        private readonly IUserManagementService _userManagementService;
        private readonly IMapper _mapper;

        public GetCommerceByIdQueryHandler(
            ICommerceRepository commerceRepository,
            IUserManagementService userManagementService,
            IMapper mapper)
        {
            _commerceRepository = commerceRepository;
            _userManagementService = userManagementService;
            _mapper = mapper;
        }

        public async Task<CommerceDetailDto> Handle(
            GetCommerceByIdQuery query, CancellationToken cancellationToken)
        {
            var commerce = await _commerceRepository.GetFirstAsync(entity => entity.Id == query.Id)
                ?? throw new NotFoundException(CommerceError.NonExistsCommerce);

            var detail = _mapper.Map<CommerceDetailDto>(commerce);

            if (!commerce.HasAssociatedUser)
                return detail;

            //El comercio vive en Persistence y su usuario en Identity: se componen aquí
            var user = await _userManagementService.GetUserByIdAsync(commerce.AssociatedUserId!);

            if (user is not null)
            {
                detail.AssociatedUser = new CommerceAssociatedUserDto
                {
                    Id = user.Id!,
                    UserName = user.UserName,
                    Email = user.Email,
                    IsActive = user.State
                };
            }

            return detail;
        }
    }
}
