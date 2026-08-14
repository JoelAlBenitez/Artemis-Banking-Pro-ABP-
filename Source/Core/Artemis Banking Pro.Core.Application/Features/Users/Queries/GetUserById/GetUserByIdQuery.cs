using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using AutoMapper;
using MediatR;

namespace Artemis_Banking_Pro.Core.Application.Features.Users.Queries.GetUserById
{
    public class GetUserByIdQuery : IRequest<UserApiDetailDto>
    {
        public required string Id { get; set; }
    }

    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserApiDetailDto>
    {
        private const string UserNotFound = "El usuario seleccionado no existe.";

        private readonly IUserManagementService _userManagementService;
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly IMapper _mapper;

        public GetUserByIdQueryHandler(
            IUserManagementService userManagementService,
            ISavingsAccountsRepository savingsAccountsRepository,
            IMapper mapper)
        {
            _userManagementService = userManagementService;
            _savingsAccountsRepository = savingsAccountsRepository;
            _mapper = mapper;
        }

        public async Task<UserApiDetailDto> Handle(
            GetUserByIdQuery query, CancellationToken cancellationToken)
        {
            var user = await _userManagementService.GetUserByIdAsync(query.Id)
                ?? throw new NotFoundException(UserNotFound);

            var detail = _mapper.Map<UserApiDetailDto>(user);

            //Solo Cliente y Comercio tienen cuenta principal
            var mainAccount = await _savingsAccountsRepository.GetFirstAsync(account =>
                account.CustomerId == query.Id &&
                account.AccountType == SavingsAccountType.Principal &&
                account.Status == SavingsAccountStatus.Activa);

            if (mainAccount is not null)
                detail.MainAccount = _mapper.Map<MainAccountDto>(mainAccount);

            return detail;
        }
    }
}
