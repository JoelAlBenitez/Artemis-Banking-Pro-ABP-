using Artemis_Banking_Pro.Core.Application.Features.Users.Commands.ChangeUserStatus;
using Artemis_Banking_Pro.Core.Application.Features.Users.Commands.CreateCommerceUser;
using Artemis_Banking_Pro.Core.Application.Features.Users.Commands.CreateUser;
using Artemis_Banking_Pro.Core.Application.Features.Users.Commands.UpdateUser;
using Artemis_Banking_Pro.Core.Application.Features.Users.Queries.GetAllUsers;
using Artemis_Banking_Pro.Core.Application.Features.Users.Queries.GetCommerceUsers;
using Artemis_Banking_Pro.Core.Application.Features.Users.Queries.GetUserById;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers
{
    [Route("api/users")]
    [Authorize(Roles = nameof(Roles.Administrador))]
    [SwaggerTag("Administración de los usuarios registrados en el sistema")]
    public class UsersController : BaseApiController
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedApiResponse<UserListItemDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [SwaggerOperation(
            Summary = "Obtener listado de usuarios",
            Description = "Listado paginado de usuarios excluyendo el rol Comercio, del más reciente al más antiguo")]
        public async Task<IActionResult> Get([FromQuery] GetAllUsersQuery query)
        {
            return Ok(await Mediator.Send(query));
        }

        [HttpGet("commerce")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedApiResponse<CommerceUserListItemDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [SwaggerOperation(
            Summary = "Obtener listado de usuarios con rol Comercio",
            Description = "Listado paginado que retorna únicamente usuarios asociados a comercios")]
        public async Task<IActionResult> GetCommerceUsers([FromQuery] GetCommerceUsersQuery query)
        {
            return Ok(await Mediator.Send(query));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserApiDetailDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Obtener detalle de usuario",
            Description = "Información detallada del usuario junto a su cuenta de ahorro principal")]
        public async Task<IActionResult> GetById(string id)
        {
            return Ok(await Mediator.Send(new GetUserByIdQuery { Id = id }));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserCreatedDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [SwaggerOperation(
            Summary = "Crear nuevo usuario",
            Description = "Crea un usuario Administrador, Cajero o Cliente. Queda inactivo y recibe su token de activación por correo")]
        public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
        {
            var user = await Mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [HttpPost("commerce/{commerceId:int}")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CommerceUserCreatedDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [SwaggerOperation(
            Summary = "Crear nuevo usuario de comercio",
            Description = "Crea un usuario con rol Comercio y lo asocia al comercio indicado. Cada comercio admite un solo usuario")]
        public async Task<IActionResult> CreateCommerceUser(
            int commerceId, [FromBody] CreateCommerceUserCommand command)
        {
            command.CommerceId = commerceId;
            var user = await Mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [SwaggerOperation(
            Summary = "Actualizar usuario",
            Description = "Modifica los datos del usuario. El rol no puede cambiarse desde este endpoint")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserCommand command)
        {
            command.Id = id;
            await Mediator.Send(command);
            return NoContent();
        }

        [HttpPatch("{id}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Cambiar estado de usuario",
            Description = "Activa o inactiva un usuario. El administrador autenticado no puede modificar su propio estado")]
        public async Task<IActionResult> ChangeStatus(string id, [FromBody] ChangeUserStatusCommand command)
        {
            command.Id = id;
            await Mediator.Send(command);
            return NoContent();
        }
    }
}
