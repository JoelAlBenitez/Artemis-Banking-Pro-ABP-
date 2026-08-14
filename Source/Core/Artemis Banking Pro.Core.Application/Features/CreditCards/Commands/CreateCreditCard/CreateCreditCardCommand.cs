using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using ArtemisBankingPro.Core.Domain.CodeErrors.CreditCardsErrors;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using AutoMapper;
using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.CreditCards.Commands.CreateCreditCard
{
    /// <summary>
    /// Datos de la tarjeta de crédito que se asignará a un cliente activo.
    /// </summary>
    /// <remarks>
    /// El número, la fecha de expiración y el CVC los genera el sistema. El CVC se almacena
    /// como hash SHA-256 y nunca se retorna.
    /// </remarks>
    public class CreateCreditCardCommand : IRequest<CreditCardListItemDto>
    {
        /// <example>20</example>
        [SwaggerParameter(Description = "Identificador del cliente al que se asignará la tarjeta")]
        public required string ClientId { get; set; }

        /// <example>50000.00</example>
        [SwaggerParameter(Description = "Límite de crédito aprobado para la tarjeta")]
        public required decimal CreditLimit { get; set; }
    }

    public class CreateCreditCardCommandValidator : AbstractValidator<CreateCreditCardCommand>
    {
        public CreateCreditCardCommandValidator()
        {
            RuleFor(command => command.ClientId)
                .NotEmpty().WithMessage(CreditCardError.CustomerRequired.Description);

            RuleFor(command => command.CreditLimit)
                .GreaterThan(0).WithMessage(CreditCardError.InvalidCreditLimitAssignment.Description);
        }
    }

    public class CreateCreditCardCommandHandler
        : IRequestHandler<CreateCreditCardCommand, CreditCardListItemDto>
    {
        private static readonly Error[] NotFoundErrors = [CreditCardError.NonExistsCustomerByIdCard];

        //No fue posible emitir un número de tarjeta único: el documento lo responde como 409
        private static readonly Error[] ConflictErrors = [CreditCardError.FailedGenerateCardNumber];

        private readonly ICreditCardsServices _creditCardsServices;
        private readonly IMapper _mapper;

        public CreateCreditCardCommandHandler(ICreditCardsServices creditCardsServices, IMapper mapper)
        {
            _creditCardsServices = creditCardsServices;
            _mapper = mapper;
        }

        public async Task<CreditCardListItemDto> Handle(
            CreateCreditCardCommand command, CancellationToken cancellationToken)
        {
            var result = await _creditCardsServices.AssignCreditCardAsync(new CreditCardAssignmentDto
            {
                CustomerId = command.ClientId,
                CreditLimit = command.CreditLimit
            });

            ValidationResultGuard.EnsureSuccess(result, NotFoundErrors, ConflictErrors);

            //El servicio confirma la asignación pero no devuelve la tarjeta: se recupera la más
            //reciente del cliente para responder el 201 del documento.
            var cardsResult = await _creditCardsServices.GetPagedCreditCardsAsync(new CreditCardFilterDto
            {
                Status = ArtemisBankingPro.Core.Domain.Common.Enum.CreditCardStatusFilter.Activas,
                Page = 1
            });

            var cards = ValidationResultGuard.EnsureSuccess(cardsResult);

            var created = cards.Items
                .Where(card => card.CustomerId == command.ClientId)
                .OrderByDescending(card => card.CreatedAt)
                .First();

            return _mapper.Map<CreditCardListItemDto>(created);
        }
    }
}
