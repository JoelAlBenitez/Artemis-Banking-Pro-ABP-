using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using ArtemisBankingPro.Core.Domain.CodeErrors.CreditCardsErrors;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.CreditCards.Commands.UpdateCreditCardLimit
{
    /// <summary>
    /// Nuevo límite de crédito de una tarjeta activa.
    /// </summary>
    public class UpdateCreditCardLimitCommand : IRequest
    {
        [SwaggerParameter(Description = "Identificador único de la tarjeta de crédito")]
        public int Id { get; set; }

        /// <example>75000.00</example>
        [SwaggerParameter(Description = "Nuevo límite de crédito. No puede ser menor que la deuda actual")]
        public required decimal CreditLimit { get; set; }
    }

    public class UpdateCreditCardLimitCommandValidator : AbstractValidator<UpdateCreditCardLimitCommand>
    {
        public UpdateCreditCardLimitCommandValidator()
        {
            RuleFor(command => command.CreditLimit)
                .GreaterThan(0).WithMessage(CreditCardError.InvalidCreditLimit.Description);
        }
    }

    public class UpdateCreditCardLimitCommandHandler : IRequestHandler<UpdateCreditCardLimitCommand>
    {
        private static readonly Error[] NotFoundErrors = [CreditCardError.NonExistsCreditCard];

        private readonly ICreditCardsServices _creditCardsServices;

        public UpdateCreditCardLimitCommandHandler(ICreditCardsServices creditCardsServices)
        {
            _creditCardsServices = creditCardsServices;
        }

        public async Task Handle(UpdateCreditCardLimitCommand command, CancellationToken cancellationToken)
        {
            //El servicio rechaza un límite menor que la deuda actual y recalcula el crédito
            //disponible antes de notificar al cliente.
            var result = await _creditCardsServices.EditCreditCardLimitAsync(new EditCardLimitDto
            {
                Id = command.Id,
                CreditLimit = command.CreditLimit
            });

            ValidationResultGuard.EnsureSuccess(result, NotFoundErrors);
        }
    }
}
