using Artemis_Banking_Pro.Core.Application.DTOs.Commerces;
using Artemis_Banking_Pro.Core.Application.Features.Commerces.Commands.ChangeCommerceStatus;
using Artemis_Banking_Pro.Core.Application.Features.Commerces.Commands.CreateCommerce;
using Artemis_Banking_Pro.Core.Application.Features.Commerces.Commands.UpdateCommerce;
using Artemis_Banking_Pro.Core.Application.Features.Commerces.Queries.GetAllCommerces;
using Artemis_Banking_Pro.Core.Application.Features.Commerces.Queries.GetCommerceById;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers
{
    [Route("api/commerce")]
    [Authorize(Roles = nameof(Roles.Administrador))]
    [SwaggerTag("Gestión de los comercios que reciben pagos mediante Hermes Pay")]
    public class CommerceController : BaseApiController
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedApiResponse<CommerceListItemDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [SwaggerOperation(
            Summary = "Obtener todos los comercios",
            Description = "Listado paginado de comercios, del más reciente al más antiguo. Por defecto muestra los activos")]
        public async Task<IActionResult> Get([FromQuery] GetAllCommercesQuery query)
        {
            return Ok(await Mediator.Send(query));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CommerceDetailDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Obtener comercio por ID",
            Description = "Información detallada del comercio y de su usuario asociado")]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await Mediator.Send(new GetCommerceByIdQuery { Id = id }));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CommerceListItemDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [SwaggerOperation(
            Summary = "Crear nuevo comercio",
            Description = "Registra el comercio en estado activo. El usuario con rol Comercio se crea desde el módulo de usuarios")]
        public async Task<IActionResult> Create([FromBody] CreateCommerceCommand command)
        {
            var commerce = await Mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = commerce.Id }, commerce);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [SwaggerOperation(
            Summary = "Actualizar comercio existente",
            Description = "Actualiza los datos del comercio. El estado no se modifica desde este endpoint")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCommerceCommand command)
        {
            command.Id = id;
            await Mediator.Send(command);
            return NoContent();
        }

        [HttpPatch("{id:int}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Cambiar estado de un comercio",
            Description = "Al desactivar, los usuarios asociados quedan inactivos. Al reactivar, no se activan automáticamente")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeCommerceStatusCommand command)
        {
            command.Id = id;
            await Mediator.Send(command);
            return NoContent();
        }
    }
}
