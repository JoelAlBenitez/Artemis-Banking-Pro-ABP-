using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Domain.CodeErrors.SavingsAccountsErrors;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.SavingsAccounts.Commands.CancelSavingsAccount
{
    /// <summary>
    /// Cancelación de una cuenta de ahorro secundaria activa.
    /// </summary>
    public class CancelSavingsAccountCommand : IRequest
    {
        [SwaggerParameter(Description = "Número identificador de 9 dígitos de la cuenta que se desea cancelar")]
        public string AccountNumber { get; set; } = string.Empty;
    }

    public class CancelSavingsAccountCommandValidator : AbstractValidator<CancelSavingsAccountCommand>
    {
        public CancelSavingsAccountCommandValidator()
        {
            RuleFor(command => command.AccountNumber)
                .NotEmpty().WithMessage("El número de cuenta es obligatorio.");
        }
    }

    public class CancelSavingsAccountCommandHandler : IRequestHandler<CancelSavingsAccountCommand>
    {
        private static readonly Error[] NotFoundErrors =
        [
            SavingsAccountError.NonExistsSavingsAccount
        ];

        private readonly ISavingsAccountsServices _savingsAccountsServices;
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;

        public CancelSavingsAccountCommandHandler(
            ISavingsAccountsServices savingsAccountsServices,
            ISavingsAccountsRepository savingsAccountsRepository)
        {
            _savingsAccountsServices = savingsAccountsServices;
            _savingsAccountsRepository = savingsAccountsRepository;
        }

        public async Task Handle(CancelSavingsAccountCommand command, CancellationToken cancellationToken)
        {
            var account = await _savingsAccountsRepository.GetFirstAsync(
                entity => entity.AccountNumber == command.AccountNumber)
                ?? throw new NotFoundException(SavingsAccountError.NonExistsSavingsAccount);

            //Cuenta principal, ya cancelada o sin principal receptora: todas son 400 según el
            //documento. El servicio decide cuál aplica y devuelve su mensaje literal.
            var result = await _savingsAccountsServices.CancelSavingsAccountAsync(account.Id);

            ValidationResultGuard.EnsureSuccess(result, NotFoundErrors);
        }
    }
}
