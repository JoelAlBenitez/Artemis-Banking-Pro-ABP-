using FluentValidation;

namespace Artemis_Banking_Pro.Core.Application.Features.Account.Commands.GetResetToken
{
    public class GetResetTokenCommandValidator : AbstractValidator<GetResetTokenCommand>
    {
        public GetResetTokenCommandValidator()
        {
            RuleFor(command => command.UserName)
                .NotEmpty().WithMessage("El nombre de usuario es obligatorio.");
        }
    }
}
