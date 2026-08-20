using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Contracts.Commerces;
using Artemis_Banking_Pro.Core.Application.DTOs.Commerces;
using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Domain.CodeErrors.CommercesErrors;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.HermesPay.Commands.ProcessPayment
{
    /// <summary>
    /// Datos del pago con tarjeta de crédito a favor de un comercio.
    /// </summary>
    public class ProcessPaymentCommand : IRequest
    {
        [SwaggerParameter(Description = "Identificador del comercio que recibirá el pago. Se ignora si el usuario autenticado tiene rol Comercio")]
        public int CommerceId { get; set; }

        /// <example>1589963258467598</example>
        [SwaggerParameter(Description = "Número de tarjeta de crédito de 16 dígitos")]
        public required string CardNumber { get; set; }

        /// <example>02</example>
        [SwaggerParameter(Description = "Mes de expiración de la tarjeta, en formato MM")]
        public required string MonthExpirationCard { get; set; }

        /// <example>2028</example>
        [SwaggerParameter(Description = "Año de expiración de la tarjeta")]
        public required string YearExpirationCard { get; set; }

        /// <example>859</example>
        [SwaggerParameter(Description = "Código de seguridad de 3 dígitos de la tarjeta")]
        public required string Cvc { get; set; }

        /// <example>689.25</example>
        [SwaggerParameter(Description = "Monto que se desea procesar como pago al comercio")]
        public required decimal TransactionAmount { get; set; }
    }

    public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand>
    {
        private static readonly Error[] NotFoundErrors = [CommerceError.NonExistsCommerce];

        private readonly ICommerceAccessService _commerceAccessService;
        private readonly IHermesPayServices _hermesPayServices;

        public ProcessPaymentCommandHandler(
            ICommerceAccessService commerceAccessService,
            IHermesPayServices hermesPayServices)
        {
            _commerceAccessService = commerceAccessService;
            _hermesPayServices = hermesPayServices;
        }

        public async Task Handle(ProcessPaymentCommand command, CancellationToken cancellationToken)
        {
            var access = await _commerceAccessService.ResolveCommerceAsync(command.CommerceId);

            //Un usuario Comercio sin comercio asociado no puede operar: es acceso denegado
            if (!access.IsValid && access.Errors.Contains(CommerceError.NonExistsCommerce)
                && command.CommerceId <= 0)
                throw new ForbiddenException(CommerceError.CommerceWithoutAssociatedUser);

            var commerce = ValidationResultGuard.EnsureSuccess(access, NotFoundErrors);

            var result = await _hermesPayServices.ProcessPaymentAsync(commerce, new ProcessPaymentDto
            {
                CardNumber = command.CardNumber,
                MonthExpirationCard = command.MonthExpirationCard,
                YearExpirationCard = command.YearExpirationCard,
                Cvc = command.Cvc,
                TransactionAmount = command.TransactionAmount
            });

            //Tarjeta inválida, vencida, comercio inactivo o crédito insuficiente son 400
            ValidationResultGuard.EnsureSuccess(result);
        }
    }
}
