using Artemis_Banking_Pro.Core.Application.DTOs.Commerces;
using Artemis_Banking_Pro.Core.Application.Features.HermesPay.Commands.ProcessPayment;
using Artemis_Banking_Pro.Core.Application.Features.HermesPay.Queries.GetCommerceTransactions;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers
{
    //Único módulo compartido por los dos roles de la API. El comercio efectivo lo resuelve
    //ICommerceAccessService: el Administrador usa el commerceId de la URL y el Comercio el
    //asociado a su token.
    [Route("pay")]
    [Authorize(Roles = $"{nameof(Roles.Administrador)},{nameof(Roles.Comercio)}")]
    [SwaggerTag("Procesador de pagos con tarjeta a favor de comercios (Hermes Pay)")]
    public class PayController : BaseApiController
    {
        [HttpGet("get-transactions/{commerceId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CommercePaymentsPageDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Obtener transacciones de un comercio",
            Description = "Listado paginado de los pagos recibidos, del más reciente al más antiguo")]
        public async Task<IActionResult> GetTransactions(
            int commerceId, [FromQuery] GetCommerceTransactionsQuery query)
        {
            query.CommerceId = commerceId;
            return Ok(await Mediator.Send(query));
        }

        [HttpPost("process-payment/{commerceId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Procesar pago de un comercio",
            Description = "Registra el consumo en la tarjeta, aumenta su deuda y acredita el monto en la cuenta principal del comercio")]
        public async Task<IActionResult> ProcessPayment(
            int commerceId, [FromBody] ProcessPaymentCommand command)
        {
            command.CommerceId = commerceId;
            await Mediator.Send(command);
            return NoContent();
        }
    }
}
