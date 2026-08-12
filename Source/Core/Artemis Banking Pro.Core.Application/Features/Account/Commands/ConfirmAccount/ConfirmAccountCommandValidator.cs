using FluentValidation;

namespace Artemis_Banking_Pro.Core.Application.Features.Account.Commands.ConfirmAccount
{
    public class ConfirmAccountCommandValidator : AbstractValidator<ConfirmAccountCommand>
    {
        public ConfirmAccountCommandValidator()
        {
            RuleFor(command => command.UserId)
                .NotEmpty().WithMessage("El identificador del usuario es obligatorio.");

            RuleFor(command => command.Token)
                .NotEmpty().WithMessage("El token de confirmación es obligatorio.");
        }
    }
}
