using FluentValidation;

namespace Artemis_Banking_Pro.Core.Application.Features.Account.Commands.ResetPassword
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(command => command.UserId)
                .NotEmpty().WithMessage("El identificador del usuario es obligatorio.");

            RuleFor(command => command.Token)
                .NotEmpty().WithMessage("El token de restablecimiento es obligatorio.");

            RuleFor(command => command.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.");

            RuleFor(command => command.ConfirmPassword)
                .NotEmpty().WithMessage("La confirmación de contraseña es obligatoria.")
                .Equal(command => command.Password)
                .WithMessage("La contraseña y la confirmación de contraseña no coinciden.");
        }
    }
}
