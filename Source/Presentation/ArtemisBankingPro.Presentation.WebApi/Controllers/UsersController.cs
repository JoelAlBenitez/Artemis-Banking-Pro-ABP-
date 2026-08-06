using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
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
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] Roles? role = null)
        {
            if (role.HasValue)
            {
                if (role.Value == Roles.Comercio)
                    return BadRequest(new { error = "No se permite consultar usuarios de tipo Comercio desde este endpoint." });

                var result = await _userManagementService.GetUsersByRoleAsync(role.Value, page, pageSize);
                return Ok(result);
            }
            else
            {
                var result = await _userManagementService.GetUsersAsync(page, pageSize, StatusFilter.Todos);
                return Ok(result);
            }
        }

        // GET /api/users/commerce
        [HttpGet("commerce")]
        public async Task<IActionResult> GetCommerceUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _userManagementService.GetCommerceUsersAsync(page, pageSize);
            return Ok(result);
        }

        // GET /api/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManagementService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { error = "Usuario no encontrado." });

            return Ok(user);
        }

        // POST /api/users
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.Equals(request.Role, Roles.Comercio.ToString(), StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "No se permite crear usuarios de tipo Comercio en este endpoint." });

            var response = await _accountRegistrationService.RegisterUserAsync(request);

            if (response.HasError)
                return BadRequest(new { error = response.Error });

            return CreatedAtAction(nameof(GetUserById), new { id = response.UserId }, response);
        }

        // POST /api/users/commerce/{commerceId}
        [HttpPost("commerce/{commerceId}")]
        public async Task<IActionResult> CreateCommerceUser(string commerceId, [FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Forzar el rol de Comercio
            request.Role = Roles.Comercio.ToString();
            var response = await _accountRegistrationService.RegisterUserAsync(request);

            if (response.HasError)
                return BadRequest(new { error = response.Error });

            return CreatedAtAction(nameof(GetUserById), new { id = response.UserId }, response);
        }

        // PUT /api/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] EditUserDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _userManagementService.UpdateUserAsync(id, request);

            if (response.HasError)
                return BadRequest(new { error = response.Error });

            return NoContent();
        }

        // PATCH /api/users/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";
            var result = await _userManagementService.ToggleUserAsync(id, currentUserId);

            if (!result)
                return BadRequest(new { error = "No fue posible cambiar el estado del usuario." });

            return NoContent();
        }
    }
}
