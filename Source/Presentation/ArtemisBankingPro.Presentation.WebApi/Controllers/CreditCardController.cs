using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using Artemis_Banking_Pro.Core.Application.Features.CreditCards.Commands.CancelCreditCard;
using Artemis_Banking_Pro.Core.Application.Features.CreditCards.Commands.CreateCreditCard;
using Artemis_Banking_Pro.Core.Application.Features.CreditCards.Commands.UpdateCreditCardLimit;
using Artemis_Banking_Pro.Core.Application.Features.CreditCards.Queries.GetAllCreditCards;
using Artemis_Banking_Pro.Core.Application.Features.CreditCards.Queries.GetCreditCardById;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers
{
    [Route("api/credit-card")]
    [Authorize(Roles = nameof(Roles.Administrador))]
    [SwaggerTag("Gestión administrativa de las tarjetas de crédito")]
    public class CreditCardController : BaseApiController
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedApiResponse<CreditCardListItemDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [SwaggerOperation(
            Summary = "Obtener tarjetas de crédito",
            Description = "Listado paginado de la más reciente a la más antigua. Solo expone los últimos cuatro dígitos")]
        public async Task<IActionResult> Get([FromQuery] GetAllCreditCardsQuery query)
        {
            return Ok(await Mediator.Send(query));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CreditCardListItemDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [SwaggerOperation(
            Summary = "Asignar tarjeta de crédito",
            Description = "Genera número, expiración y CVC hasheado. La tarjeta queda activa con deuda inicial RD$0.00")]
        public async Task<IActionResult> Create([FromBody] CreateCreditCardCommand command)
        {
            var card = await Mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CreditCardDetailDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Ver detalles de una tarjeta",
            Description = "Información de la tarjeta y sus consumos aprobados y rechazados, del más reciente al más antiguo")]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await Mediator.Send(new GetCreditCardByIdQuery { Id = id }));
        }

        [HttpPatch("{id:int}/limit")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Editar límite de una tarjeta",
            Description = "El nuevo límite no puede ser menor que la deuda actual. Recalcula el crédito disponible")]
        public async Task<IActionResult> UpdateLimit(int id, [FromBody] UpdateCreditCardLimitCommand command)
        {
            command.Id = id;
            await Mediator.Send(command);
            return NoContent();
        }

        [HttpPatch("{id:int}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Cancelar tarjeta de crédito",
            Description = "Solo cancela tarjetas activas sin deuda pendiente. El historial de consumos se conserva")]
        public async Task<IActionResult> Cancel(int id)
        {
            await Mediator.Send(new CancelCreditCardCommand { Id = id });
            return NoContent();
        }
    }
}
