using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Domain.CodeErrors.SavingsAccountsErrors;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using AutoMapper;
using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.SavingsAccounts.Queries.GetAccountTransactions
{
    /// <summary>
    /// Historial de transacciones de una cuenta de ahorro.
    /// </summary>
    public class GetAccountTransactionsQuery : IRequest<AccountTransactionsDto>
    {
        [SwaggerParameter(Description = "Número identificador de 9 dígitos de la cuenta de ahorro")]
        public string AccountNumber { get; set; } = string.Empty;

        /// <example>1</example>
        [SwaggerParameter(Description = "Número de página que se desea consultar")]
        public int Page { get; set; } = 1;

        /// <example>20</example>
        [SwaggerParameter(Description = "Cantidad de transacciones por página. Máximo 20")]
        public int PageSize { get; set; } = DomainConstants.DefaultPageSize;
    }

    public class GetAccountTransactionsQueryValidator : AbstractValidator<GetAccountTransactionsQuery>
    {
        public GetAccountTransactionsQueryValidator()
        {
            RuleFor(query => query.AccountNumber)
                .NotEmpty().WithMessage("El número de cuenta es obligatorio.");

            RuleFor(query => query.Page).ValidPage();
            RuleFor(query => query.PageSize).ValidPageSize();
        }
    }

    public class GetAccountTransactionsQueryHandler
        : IRequestHandler<GetAccountTransactionsQuery, AccountTransactionsDto>
    {
        private readonly ISavingsAccountsServices _savingsAccountsServices;
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly IUserManagementService _userManagementService;
        private readonly IMapper _mapper;

        public GetAccountTransactionsQueryHandler(
            ISavingsAccountsServices savingsAccountsServices,
            ISavingsAccountsRepository savingsAccountsRepository,
            IUserManagementService userManagementService,
            IMapper mapper)
        {
            _savingsAccountsServices = savingsAccountsServices;
            _savingsAccountsRepository = savingsAccountsRepository;
            _userManagementService = userManagementService;
            _mapper = mapper;
        }

        public async Task<AccountTransactionsDto> Handle(
            GetAccountTransactionsQuery query, CancellationToken cancellationToken)
        {
            //La API identifica la cuenta por su número de 9 dígitos; el servicio trabaja con
            //el identificador técnico.
            var account = await _savingsAccountsRepository.GetFirstAsync(
                entity => entity.AccountNumber == query.AccountNumber)
                ?? throw new NotFoundException(SavingsAccountError.NonExistsSavingsAccount);

            var result = await _savingsAccountsServices.GetPagedTransactionsAsync(
                account.Id, query.Page, query.PageSize);

            var transactions = ValidationResultGuard.EnsureSuccess(result,
                [SavingsAccountError.NonExistsSavingsAccount]);

            var fullName = await _userManagementService.GetFullNameByIdAsync(account.CustomerId);

            return new AccountTransactionsDto
            {
                AccountNumber = account.AccountNumber,
                ClientFullName = fullName ?? string.Empty,
                Balance = account.Balance,
                Type = account.AccountType.ToString(),
                Status = account.Status.ToString(),
                Transactions = PagedApiResponse<TransactionApiDto>.From(
                    transactions, transaction => _mapper.Map<TransactionApiDto>(transaction))
            };
        }
    }
}
