using FluentValidation;

namespace Artemis_Banking_Pro.Core.Application.Features.Account.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(command => command.UserName)
                .NotEmpty().WithMessage("El nombre de usuario es obligatorio.");

            RuleFor(command => command.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.");
        }
    }
}
