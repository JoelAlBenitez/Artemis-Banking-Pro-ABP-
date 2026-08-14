using ArtemisBankingPro.Core.Domain.Common.Constants;
using FluentValidation;

namespace Artemis_Banking_Pro.Core.Application.Features.HermesPay.Commands.ProcessPayment
{
    public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
    {
        public ProcessPaymentCommandValidator()
        {
            RuleFor(command => command.CardNumber)
                .NotEmpty().WithMessage("El número de tarjeta es requerido.")
                .Length(DomainConstants.CardNumberLength)
                .WithMessage($"El número de tarjeta debe contener exactamente {DomainConstants.CardNumberLength} dígitos.")
                .Matches("^[0-9]+$")
                .WithMessage("El número de tarjeta solo puede contener dígitos.");

            RuleFor(command => command.MonthExpirationCard)
                .NotEmpty().WithMessage("El mes de expiración es requerido.")
                .Must(BeAValidMonth)
                .WithMessage("El mes de expiración debe tener un valor válido entre 01 y 12.");

            RuleFor(command => command.YearExpirationCard)
                .NotEmpty().WithMessage("El año de expiración es requerido.");

            RuleFor(command => command.Cvc)
                .NotEmpty().WithMessage("El CVC es requerido.")
                .Length(DomainConstants.CvcLength)
                .WithMessage($"El CVC debe contener exactamente {DomainConstants.CvcLength} dígitos.")
                .Matches("^[0-9]+$")
                .WithMessage("El CVC solo puede contener dígitos.");

            RuleFor(command => command.TransactionAmount)
                .GreaterThan(0)
                .WithMessage("El monto de la transacción debe ser mayor que cero.");
        }

        private static bool BeAValidMonth(string month)
            => int.TryParse(month, out var value) && value is >= 1 and <= 12;
    }
}
