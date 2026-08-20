using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.CodeErrors.CommercesErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Users.Commands.CreateCommerceUser
{
    /// <summary>
    /// Datos del usuario que operará un comercio. El rol no se recibe: siempre es Comercio.
    /// </summary>
    public class CreateCommerceUserCommand : IRequest<CommerceUserCreatedDto>
    {
        [SwaggerParameter(Description = "Identificador del comercio al que se asociará el usuario")]
        public int CommerceId { get; set; }

        /// <example>Usuario</example>
        [SwaggerParameter(Description = "Nombre del usuario de comercio")]
        public required string FirstName { get; set; }

        /// <example>Comercio</example>
        [SwaggerParameter(Description = "Apellido del usuario de comercio")]
        public required string LastName { get; set; }

        /// <example>10199999999</example>
        [SwaggerParameter(Description = "Cédula o identificador del usuario")]
        public required string Identification { get; set; }

        /// <example>commerce01@artemis.com</example>
        [SwaggerParameter(Description = "Correo electrónico del usuario")]
        public required string Email { get; set; }

        /// <example>commerce01</example>
        [SwaggerParameter(Description = "Nombre de usuario para iniciar sesión en la API")]
        public required string UserName { get; set; }

        /// <example>123P@$$word!</example>
        [SwaggerParameter(Description = "Contraseña inicial del usuario")]
        public required string Password { get; set; }

        /// <example>123P@$$word!</example>
        [SwaggerParameter(Description = "Confirmación de la contraseña inicial")]
        public required string ConfirmPassword { get; set; }

        /// <example>0.00</example>
        [SwaggerParameter(Description = "Balance inicial de la cuenta de ahorro principal del comercio")]
        public required decimal InitialAmount { get; set; }
    }

    public class CreateCommerceUserCommandHandler
        : IRequestHandler<CreateCommerceUserCommand, CommerceUserCreatedDto>
    {
        private readonly IAccountRegistrationService _accountRegistrationService;
        private readonly ICommerceRepository _commerceRepository;
        private readonly ICurrentUserService _currentUserService;

        public CreateCommerceUserCommandHandler(
            IAccountRegistrationService accountRegistrationService,
            ICommerceRepository commerceRepository,
            ICurrentUserService currentUserService)
        {
            _accountRegistrationService = accountRegistrationService;
            _commerceRepository = commerceRepository;
            _currentUserService = currentUserService;
        }

        public async Task<CommerceUserCreatedDto> Handle(
            CreateCommerceUserCommand command, CancellationToken cancellationToken)
        {
            var commerce = await _commerceRepository.GetByIdAsync(command.CommerceId)
                ?? throw new NotFoundException(CommerceError.NonExistsCommerce);

            if (commerce.HasAssociatedUser)
                throw new ConflictException(CommerceError.CommerceAlreadyHasUser);

            var response = await _accountRegistrationService.RegisterUserAsync(new RegisterRequest
            {
                FirstName = command.FirstName,
                LastName = command.LastName,
                IDCARD = command.Identification,
                Email = command.Email,
                UserName = command.UserName,
                Password = command.Password,
                ConfirmPassword = command.ConfirmPassword,
                Role = nameof(Roles.Comercio),
                InitialAmount = command.InitialAmount,
                Origin = null
            });

            if (response.HasError)
            {
                throw response.Conflict
                    ? new ConflictException(response.Error!)
                    : (Exception)new BusinessRuleException(response.Error!);
            }

            //El usuario vive en Identity y el comercio en Persistence: la asociación se cierra
            //aquí, después de que el usuario existe.
            commerce.AssociatedUserId = response.UserId;
            commerce.LastModifiedByIdUser = _currentUserService.UserId;
            commerce.ModifiedAt = DateTimeOffset.UtcNow;

            await _commerceRepository.UpdateAsync(commerce);
            await _commerceRepository.SaveChangesAsync();

            return new CommerceUserCreatedDto
            {
                Id = response.UserId!,
                UserName = command.UserName,
                Email = command.Email,
                Role = nameof(Roles.Comercio),
                CommerceId = commerce.Id,
                IsActive = false
            };
        }
    }
}
