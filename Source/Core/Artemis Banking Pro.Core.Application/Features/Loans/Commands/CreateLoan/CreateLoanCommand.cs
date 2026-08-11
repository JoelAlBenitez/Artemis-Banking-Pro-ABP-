using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Domain.CodeErrors.LoansErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Artemis_Banking_Pro.Core.Application.Features.Loans.Commands.CreateLoan
{
    /// <summary>
    /// Datos del préstamo que se asignará a un cliente activo.
    /// </summary>
    public class CreateLoanCommand : IRequest<LoanAssignmentResultDto>
    {
        /// <example>20</example>
        [SwaggerParameter(Description = "Identificador del cliente al que se asignará el préstamo")]
        public required string ClientId { get; set; }

        /// <example>100000.00</example>
        [SwaggerParameter(Description = "Monto de capital aprobado para el préstamo")]
        public required decimal CapitalAmount { get; set; }

        /// <example>12</example>
        [SwaggerParameter(Description = "Plazo en meses. Valores permitidos: 6, 12, 18, 24, 30, 36, 42, 48, 54 y 60")]
        public required int TermInMonths { get; set; }

        /// <example>12.00</example>
        [SwaggerParameter(Description = "Tasa de interés anual aplicada al préstamo")]
        public required decimal AnnualInterestRate { get; set; }

        /// <example>false</example>
        [SwaggerParameter(Description = "Confirma la asignación aunque el cliente sea de alto riesgo")]
        public bool ConfirmHighRisk { get; set; }
    }

    public class CreateLoanCommandHandler : IRequestHandler<CreateLoanCommand, LoanAssignmentResultDto>
    {
        private static readonly Error[] NotFoundErrors = [LoansError.NonExistsCustomerByIdCard];

        private static readonly Error[] ConflictErrors =
        [
            LoansError.CustomerWithLoanExist,
            LoansError.FaildGenerateLoansInstallment
        ];

        private readonly ILoansServices _loansServices;
        private readonly IUserManagementService _userManagementService;

        public CreateLoanCommandHandler(
            ILoansServices loansServices,
            IUserManagementService userManagementService)
        {
            _loansServices = loansServices;
            _userManagementService = userManagementService;
        }

        public async Task<LoanAssignmentResultDto> Handle(
            CreateLoanCommand command, CancellationToken cancellationToken)
        {
            var assignment = new LoansAssignmentDto
            {
                CustomerId = command.ClientId,
                AmmountLoans = command.CapitalAmount,
                TermLoans = (TermMonths)command.TermInMonths,
                AnnualInterestRate = command.AnnualInterestRate,
                ConfirmHighRisk = command.ConfirmHighRisk
            };

            //El riesgo se evalúa antes de crear: sin confirmación, el documento exige responder
            //409 con los montos, y esos datos no caben en una excepción de negocio.
            var risk = await _loansServices.EvaluateRiskAsync(assignment);
            var evaluation = ValidationResultGuard.EnsureSuccess(risk, NotFoundErrors);

            if (evaluation.RequiresConfirmation && !command.ConfirmHighRisk)
            {
                return new LoanAssignmentResultDto
                {
                    HighRisk = new HighRiskConflictDto
                    {
                        Message = evaluation.Message,
                        RiskType = evaluation.RiskType.ToString(),
                        CurrentDebt = evaluation.CurrentDebt,
                        ProjectedDebt = evaluation.ProjectedDebt,
                        AverageDebt = evaluation.AverageDebt
                    }
                };
            }

            var result = await _loansServices.CreateAsync(assignment);
            ValidationResultGuard.EnsureSuccess(result, NotFoundErrors, ConflictErrors);

            return new LoanAssignmentResultDto { Loan = await BuildCreatedLoanAsync(command.ClientId) };
        }

        //CreateAsync confirma la asignación pero no devuelve el préstamo: se recupera el activo
        //del cliente para responder el 201 con su cuota mensual y su total a pagar.
        private async Task<LoanCreatedDto> BuildCreatedLoanAsync(string clientId)
        {
            var activeLoans = await _loansServices.GetActiveLoansByCustomerAsync(clientId);
            var loan = ValidationResultGuard.EnsureSuccess(activeLoans)
                .OrderByDescending(active => active.CreatedAt)
                .First();

            var detailResult = await _loansServices.GetDetailLoanAsync(loan.Id);
            var detail = ValidationResultGuard.EnsureSuccess(detailResult);

            var installments = detail.loansInstallmentDtos;

            return new LoanCreatedDto
            {
                Id = loan.Id,
                LoanNumber = loan.LoanNumber,
                ClientId = loan.CustomerId,
                ClientFullName = await _userManagementService.GetFullNameByIdAsync(clientId) ?? string.Empty,
                CapitalAmount = loan.AprovechedCapital,
                TermInMonths = loan.Term,
                AnnualInterestRate = loan.AnnualInterestRate,
                MonthlyInstallment = installments.Count == 0 ? 0m : installments[0].InstallmentValue,
                TotalAmountToPay = installments.Sum(installment => installment.InstallmentValue),
                Status = loan.StateLoans.ToString(),
                CreatedAt = loan.CreatedAt
            };
        }
    }
}
