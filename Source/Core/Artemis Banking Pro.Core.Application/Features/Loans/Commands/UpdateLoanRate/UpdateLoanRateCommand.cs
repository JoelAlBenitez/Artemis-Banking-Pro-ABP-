using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using ArtemisBankingPro.Core.Domain.CodeErrors.LoansErros;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Loans.Commands.UpdateLoanRate
{
    /// <summary>
    /// Nueva tasa de interés anual de un préstamo activo.
    /// </summary>
    public class UpdateLoanRateCommand : IRequest
    {
        [SwaggerParameter(Description = "Identificador del préstamo que se desea modificar")]
        public int Id { get; set; }

        /// <example>10.50</example>
        [SwaggerParameter(Description = "Nueva tasa de interés anual que será aplicada al préstamo")]
        public required decimal AnnualInterestRate { get; set; }
    }

    public class UpdateLoanRateCommandValidator : AbstractValidator<UpdateLoanRateCommand>
    {
        public UpdateLoanRateCommandValidator()
        {
            RuleFor(command => command.AnnualInterestRate)
                .GreaterThanOrEqualTo(0)
                .WithMessage(LoansError.NegativeAnnualInterestRate.Description);
        }
    }

    public class UpdateLoanRateCommandHandler : IRequestHandler<UpdateLoanRateCommand>
    {
        private static readonly Error[] NotFoundErrors = [LoansError.NonExistsLoan];

        private readonly ILoansServices _loansServices;

        public UpdateLoanRateCommandHandler(ILoansServices loansServices)
        {
            _loansServices = loansServices;
        }

        public async Task Handle(UpdateLoanRateCommand command, CancellationToken cancellationToken)
        {
            //El servicio recalcula únicamente las cuotas futuras pendientes: las pagadas,
            //parcialmente pagadas, vencidas y las que vencen hoy no se tocan.
            var result = await _loansServices.EditAnnualInterestRateAsync(new EditAnnualInterestRateDto
            {
                Id = command.Id,
                AnnualInterestRate = command.AnnualInterestRate
            });

            ValidationResultGuard.EnsureSuccess(result, NotFoundErrors);
        }
    }
}
