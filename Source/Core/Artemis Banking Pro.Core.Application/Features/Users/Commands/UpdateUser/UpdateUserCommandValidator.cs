using FluentValidation;

namespace Artemis_Banking_Pro.Core.Application.Features.Users.Commands.UpdateUser
{
    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(command => command.Id)
                .NotEmpty().WithMessage("El identificador del usuario es obligatorio.");

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

            //La contraseña es opcional, pero si se envía debe venir con su confirmación
            RuleFor(command => command.ConfirmPassword)
                .NotEmpty().WithMessage("La confirmación de contraseña es obligatoria.")
                .Equal(command => command.Password)
                .WithMessage("La contraseña y la confirmación de contraseña no coinciden.")
                .When(command => !string.IsNullOrWhiteSpace(command.Password));

            RuleFor(command => command.AdditionalAmount)
                .GreaterThanOrEqualTo(0)
                .When(command => command.AdditionalAmount.HasValue)
                .WithMessage("El monto adicional no puede ser negativo.");
        }
    }
}
