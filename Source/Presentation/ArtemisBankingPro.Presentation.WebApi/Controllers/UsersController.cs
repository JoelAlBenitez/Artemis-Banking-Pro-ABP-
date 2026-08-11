using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = nameof(Roles.Administrador))]
    public class UsersController : ControllerBase
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IAccountRegistrationService _accountRegistrationService;

        public UsersController(
            IUserManagementService userManagementService,
            IAccountRegistrationService accountRegistrationService)
        {
            _userManagementService = userManagementService;
            _accountRegistrationService = accountRegistrationService;
        }

        // GET /api/users
        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = DomainConstants.DefaultPageSize,
            [FromQuery] string? role = null)
        {
            var paginationError = ValidatePagination(page, pageSize);
            if (paginationError != null) return paginationError;

            if (string.IsNullOrWhiteSpace(role))
                return Ok(await _userManagementService.GetUsersAsync(page, pageSize, StatusFilter.Todos));

            //Solo se admiten los tres roles de la aplicación web: Comercio tiene su propio endpoint
            if (!Enum.TryParse<Roles>(role, true, out var roleEnum) || roleEnum == Roles.Comercio)
                return BadRequest(new { error = "El tipo de usuario solo puede ser Administrador, Cajero o Cliente." });

            return Ok(await _userManagementService.GetUsersByRoleAsync(roleEnum, page, pageSize));
        }

        // GET /api/users/commerce
        [HttpGet("commerce")]
        public async Task<IActionResult> GetCommerceUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = DomainConstants.DefaultPageSize)
        {
            var paginationError = ValidatePagination(page, pageSize);
            if (paginationError != null) return paginationError;

            return Ok(await _userManagementService.GetCommerceUsersAsync(page, pageSize));
        }

        // GET /api/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManagementService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { error = "El usuario seleccionado no existe." });

            return Ok(user);
        }

        // POST /api/users
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            //Los usuarios de comercio se crean únicamente desde su endpoint dedicado
            if (string.Equals(request.Role, nameof(Roles.Comercio), StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "No se permite crear usuarios de tipo Comercio desde este endpoint." });

            //El correo de la API lleva el token, no un enlace: no se envía origen
            request.Origin = null;

            var response = await _accountRegistrationService.RegisterUserAsync(request);
            if (response.HasError)
                return response.Conflict
                    ? Conflict(new { error = response.Error })
                    : BadRequest(new { error = response.Error });

            return CreatedAtAction(nameof(GetUserById), new { id = response.UserId }, new
            {
                id = response.UserId,
                userName = request.UserName,
                email = request.Email,
                role = request.Role,
                isActive = false
            });
        }

        // POST /api/users/commerce/{commerceId}
        [HttpPost("commerce/{commerceId:int}")]
        public async Task<IActionResult> CreateCommerceUser(int commerceId, [FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            //Pendiente de integración: la validación de existencia del comercio y de que no
            //tenga otro usuario asociado corresponde al módulo de Gestión de Comercios,
            //que aún no expone su contrato. Sin él no pueden emitirse los 404 / 409 del documento.
            request.Role = nameof(Roles.Comercio);
            request.Origin = null;

            var response = await _accountRegistrationService.RegisterUserAsync(request);
            if (response.HasError)
                return response.Conflict
                    ? Conflict(new { error = response.Error })
                    : BadRequest(new { error = response.Error });

            return CreatedAtAction(nameof(GetUserById), new { id = response.UserId }, new
            {
                id = response.UserId,
                userName = request.UserName,
                email = request.Email,
                role = nameof(Roles.Comercio),
                commerceId,
                isActive = false
            });
        }

        // PUT /api/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] EditUserDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _userManagementService.UpdateUserAsync(id, request);
            if (!response.HasError) return NoContent();

            if (response.NotFound) return NotFound(new { error = response.Error });
            if (response.Conflict) return Conflict(new { error = response.Error });

            return BadRequest(new { error = response.Error });
        }

        // PATCH /api/users/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeUserStatus(string id, [FromBody] ChangeUserStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            //La cuenta propia la rechaza el servicio: aquí no se lee el claim del autenticado
            var response = await _userManagementService.SetUserStatusAsync(id, request.Status);

            if (!response.HasError) return NoContent();
            if (response.NotFound) return NotFound(new { error = response.Error });

            //El intento de auto-modificación se responde como acceso denegado
            return StatusCode(StatusCodes.Status403Forbidden, new { error = response.Error });
        }

        //Ningún listado administrativo devuelve más de 20 registros por página
        private IActionResult? ValidatePagination(int page, int pageSize)
        {
            if (page < 1)
                return BadRequest(new { error = "El número de página debe ser mayor que cero." });

            if (pageSize < 1)
                return BadRequest(new { error = "La cantidad de registros por página debe ser mayor que cero." });

            if (pageSize > DomainConstants.MaxPageSize)
                return BadRequest(new { error = $"La cantidad máxima de registros por página es {DomainConstants.MaxPageSize}." });

            return null;
        }
    }

    public class ChangeUserStatusRequest
    {
        //true activa el usuario, false lo inactiva
        public required bool Status { get; set; }
    }
}
