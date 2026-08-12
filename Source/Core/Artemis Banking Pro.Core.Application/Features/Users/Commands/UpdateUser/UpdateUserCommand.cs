using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Users.Commands.UpdateUser
{
    /// <summary>
    /// Datos actualizables de un usuario. El rol no puede modificarse.
    /// </summary>
    public class UpdateUserCommand : IRequest
    {
        [SwaggerParameter(Description = "Identificador del usuario que se desea actualizar")]
        public string Id { get; set; } = string.Empty;

        /// <example>María</example>
        [SwaggerParameter(Description = "Nombre del usuario")]
        public required string FirstName { get; set; }

        /// <example>Gómez</example>
        [SwaggerParameter(Description = "Apellido del usuario")]
        public required string LastName { get; set; }

        /// <example>00187654321</example>
        [SwaggerParameter(Description = "Cédula del usuario")]
        public required string Identification { get; set; }

        /// <example>maria.gomez@artemis.com</example>
        [SwaggerParameter(Description = "Correo electrónico del usuario")]
        public required string Email { get; set; }

        /// <example>cliente01</example>
        [SwaggerParameter(Description = "Nombre de usuario")]
        public required string UserName { get; set; }

        /// <example>123P@$$word!</example>
        [SwaggerParameter(Description = "Nueva contraseña. Solo se modifica si se envía este campo")]
        public string? Password { get; set; }

        [SwaggerParameter(Description = "Confirmación de la nueva contraseña. Obligatoria solo si se envía la contraseña")]
        public string? ConfirmPassword { get; set; }

        /// <example>12000.00</example>
        [SwaggerParameter(Description = "Monto adicional a sumar a la cuenta principal si el usuario es Cliente o Comercio")]
        public decimal? AdditionalAmount { get; set; }
    }

    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
    {
        private readonly IUserManagementService _userManagementService;

        public UpdateUserCommandHandler(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        public async Task Handle(UpdateUserCommand command, CancellationToken cancellationToken)
        {
            var response = await _userManagementService.UpdateUserAsync(command.Id, new EditUserDto
            {
                Id = command.Id,
                Name = command.FirstName,
                LastName = command.LastName,
                IDCARD = command.Identification,
                Email = command.Email,
                UserName = command.UserName,
                NewPassword = command.Password,
                ConfirmNewPassword = command.ConfirmPassword,
                AdditionalAmount = command.AdditionalAmount
            });

            if (!response.HasError)
                return;

            if (response.NotFound)
                throw new NotFoundException(response.Error!);

            if (response.Conflict)
                throw new ConflictException(response.Error!);

            throw new BusinessRuleException(response.Error!);
        }
    }
}
