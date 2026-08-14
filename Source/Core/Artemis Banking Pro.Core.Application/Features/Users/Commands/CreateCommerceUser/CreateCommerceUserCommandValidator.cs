using FluentValidation;

namespace Artemis_Banking_Pro.Core.Application.Features.Users.Commands.CreateCommerceUser
{
    public class CreateCommerceUserCommandValidator : AbstractValidator<CreateCommerceUserCommand>
    {
        public CreateCommerceUserCommandValidator()
        {
            RuleFor(command => command.CommerceId)
                .GreaterThan(0).WithMessage("El identificador del comercio no es válido.");

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

            RuleFor(command => command.InitialAmount)
                .GreaterThanOrEqualTo(0)
                .WithMessage("El balance inicial no puede ser negativo.");
        }
    }
}
