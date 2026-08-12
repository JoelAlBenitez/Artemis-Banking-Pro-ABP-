using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Domain.CodeErrors.SavingsAccountsErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.SavingsAccounts.Commands.CreateSecondaryAccount
{
    /// <summary>
    /// Datos de la cuenta de ahorro secundaria que se asignará a un cliente activo.
    /// </summary>
    public class CreateSecondaryAccountCommand : IRequest<SavingsAccountCreatedDto>
    {
        /// <example>20</example>
        [SwaggerParameter(Description = "Identificador del cliente al que se asignará la cuenta secundaria")]
        public required string ClientId { get; set; }

        /// <example>5000.00</example>
        [SwaggerParameter(Description = "Balance inicial de la cuenta. Puede ser RD$0.00, pero no negativo")]
        public required decimal InitialBalance { get; set; }
    }

    public class CreateSecondaryAccountCommandValidator : AbstractValidator<CreateSecondaryAccountCommand>
    {
        public CreateSecondaryAccountCommandValidator()
        {
            RuleFor(command => command.ClientId)
                .NotEmpty().WithMessage(SavingsAccountError.CustomerRequired.Description);

            RuleFor(command => command.InitialBalance)
                .GreaterThanOrEqualTo(0)
                .WithMessage(SavingsAccountError.NegativeInitialBalance.Description);
        }
    }

    public class CreateSecondaryAccountCommandHandler
        : IRequestHandler<CreateSecondaryAccountCommand, SavingsAccountCreatedDto>
    {
        private static readonly Error[] NotFoundErrors =
        [
            SavingsAccountError.NonExistsCustomerByIdCard
        ];

        //No fue posible emitir un número de cuenta único: el documento lo responde como 409
        private static readonly Error[] ConflictErrors =
        [
            SavingsAccountError.FailedGenerateAccountNumber
        ];

        private readonly ISavingsAccountsServices _savingsAccountsServices;
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly IUserManagementService _userManagementService;

        public CreateSecondaryAccountCommandHandler(
            ISavingsAccountsServices savingsAccountsServices,
            ISavingsAccountsRepository savingsAccountsRepository,
            IUserManagementService userManagementService)
        {
            _savingsAccountsServices = savingsAccountsServices;
            _savingsAccountsRepository = savingsAccountsRepository;
            _userManagementService = userManagementService;
        }

        public async Task<SavingsAccountCreatedDto> Handle(
            CreateSecondaryAccountCommand command, CancellationToken cancellationToken)
        {
            var result = await _savingsAccountsServices.AssignSavingsAccountAsync(new SavingsAccountAssignmentDto
            {
                CustomerId = command.ClientId,
                InitialBalance = command.InitialBalance
            });

            ValidationResultGuard.EnsureSuccess(result, NotFoundErrors, ConflictErrors);

            //El servicio confirma la asignación pero no devuelve la cuenta creada: se recupera
            //la secundaria más reciente del cliente para responder el 201 del documento.
            var accounts = await _savingsAccountsRepository.GetAllFindAsync(account =>
                account.CustomerId == command.ClientId &&
                account.AccountType == SavingsAccountType.Secundaria);

            var created = accounts.OrderByDescending(account => account.CreatedAt).First();

            var fullName = await _userManagementService.GetFullNameByIdAsync(command.ClientId);

            return new SavingsAccountCreatedDto
            {
                Id = created.Id,
                AccountNumber = created.AccountNumber,
                ClientId = created.CustomerId,
                ClientFullName = fullName ?? string.Empty,
                Balance = created.Balance,
                Type = created.AccountType.ToString(),
                Status = created.Status.ToString(),
                CreatedAt = created.CreatedAt
            };
        }
    }
}
