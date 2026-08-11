using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using ArtemisBankingPro.Core.Domain.CodeErrors.CreditCardsErrors;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.CreditCards.Commands.CancelCreditCard
{
    /// <summary>
    /// Cancelación de una tarjeta de crédito activa sin deuda pendiente.
    /// </summary>
    public class CancelCreditCardCommand : IRequest
    {
        [SwaggerParameter(Description = "Identificador único de la tarjeta que se desea cancelar")]
        public int Id { get; set; }
    }

    public class CancelCreditCardCommandHandler : IRequestHandler<CancelCreditCardCommand>
    {
        private static readonly Error[] NotFoundErrors = [CreditCardError.NonExistsCreditCard];

        private readonly ICreditCardsServices _creditCardsServices;

        public CancelCreditCardCommandHandler(ICreditCardsServices creditCardsServices)
        {
            _creditCardsServices = creditCardsServices;
        }

        public async Task Handle(CancelCreditCardCommand command, CancellationToken cancellationToken)
        {
            //Tarjeta ya cancelada o con deuda pendiente son 400 según el documento; el servicio
            //devuelve el mensaje literal que corresponde a cada caso.
            var result = await _creditCardsServices.CancelCreditCardAsync(command.Id);

            ValidationResultGuard.EnsureSuccess(result, NotFoundErrors);
        }
    }
}
