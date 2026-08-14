using Artemis_Banking_Pro.Core.Application.Common;
using FluentValidation;

namespace Artemis_Banking_Pro.Core.Application.Features.Users.Commands.CreateUser
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(command => command.FirstName)
                .NotEmpty().WithMessage("El nombre es obligatorio.");

            RuleFor(command => command.LastName)
                .NotEmpty().WithMessage("El apellido es obligatorio.");

            RuleFor(command => command.Identification)
                .NotEmpty().WithMessage("La cédula es obligatoria.");

            RuleFor(command => command.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
                .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.");

            RuleFor(command => command.UserName)
                .NotEmpty().WithMessage("El nombre de usuario es obligatorio.");

            RuleFor(command => command.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.");

            RuleFor(command => command.ConfirmPassword)
                .NotEmpty().WithMessage("La confirmación de contraseña es obligatoria.")
                .Equal(command => command.Password)
                .WithMessage("La contraseña y la confirmación de contraseña no coinciden.");

            //El rol Comercio se crea únicamente desde POST /api/users/commerce/{commerceId}
            RuleFor(command => command.Role)
                .NotEmpty().WithMessage("El rol es obligatorio.")
                .Must(ApiFilterValues.User.IsAllowedRole)
                .WithMessage("El rol solo puede ser Administrador, Cajero o Cliente.");

            RuleFor(command => command.InitialAmount)
                .GreaterThanOrEqualTo(0)
                .When(command => command.InitialAmount.HasValue)
                .WithMessage("El monto inicial no puede ser negativo.");
        }
    }
}
