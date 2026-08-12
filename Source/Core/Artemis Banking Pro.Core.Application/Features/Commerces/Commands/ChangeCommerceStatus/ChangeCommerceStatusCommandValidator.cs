using FluentValidation;

namespace Artemis_Banking_Pro.Core.Application.Features.Commerces.Commands.ChangeCommerceStatus
{
    public class ChangeCommerceStatusCommandValidator : AbstractValidator<ChangeCommerceStatusCommand>
    {
        public ChangeCommerceStatusCommandValidator()
        {
            RuleFor(command => command.Id)
                .GreaterThan(0).WithMessage("El identificador del comercio no es válido.");
        }
    }
}
