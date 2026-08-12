using ArtemisBankingPro.Core.Domain.CodeErrors.LoansErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using FluentValidation;

namespace Artemis_Banking_Pro.Core.Application.Features.Loans.Commands.CreateLoan
{
    public class CreateLoanCommandValidator : AbstractValidator<CreateLoanCommand>
    {
        public CreateLoanCommandValidator()
        {
            RuleFor(command => command.ClientId)
                .NotEmpty().WithMessage("El cliente es obligatorio.");

            RuleFor(command => command.CapitalAmount)
                .GreaterThan(0).WithMessage("El monto del préstamo debe ser mayor que cero.");

            RuleFor(command => command.AnnualInterestRate)
                .GreaterThanOrEqualTo(0)
                .WithMessage(LoansError.NegativeAnnualInterestRate.Description);

            //Solo los diez plazos que fija el documento funcional
            RuleFor(command => command.TermInMonths)
                .Must(term => System.Enum.IsDefined(typeof(TermMonths), term))
                .WithMessage(LoansError.InvalidTerm.Description);
        }
    }
}
