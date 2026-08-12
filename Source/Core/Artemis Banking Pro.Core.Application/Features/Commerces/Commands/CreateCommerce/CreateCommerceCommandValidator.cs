using FluentValidation;

namespace Artemis_Banking_Pro.Core.Application.Features.Commerces.Commands.CreateCommerce
{
    public class CreateCommerceCommandValidator : AbstractValidator<CreateCommerceCommand>
    {
        public CreateCommerceCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty().WithMessage("El nombre del comercio es obligatorio.");

            RuleFor(command => command.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
                .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.");

            RuleFor(command => command.PhoneNumber)
                .NotEmpty().WithMessage("El teléfono es obligatorio.");

            RuleFor(command => command.Rnc)
                .NotEmpty().WithMessage("El RNC es obligatorio.");
        }
    }
}
