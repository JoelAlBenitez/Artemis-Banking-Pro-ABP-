using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.Loans
{
    /// Control automático de cuotas atrasadas de los préstamos activos.
    /// Reutiliza el repositorio del dominio; no crea acceso a datos propio.
    public sealed class LoansOverdueServices : ILoansOverdueServices
    {
        private readonly ILoanInstallmentRepository _loanInstallmentRepository;
        private readonly ILogger<LoansOverdueServices> _logger;

        public LoansOverdueServices(
            ILoanInstallmentRepository loanInstallmentRepository,
            ILogger<LoansOverdueServices> logger)
        {
            _loanInstallmentRepository = loanInstallmentRepository;
            _logger = logger;
        }

        public async Task<ValidationResult<OverdueInstallmentsResultDto>> ReviewOverdueInstallmentsAsync()
        {
            try
            {
                var today = DateTimeOffset.UtcNow;

                #region cuotas de prestamos activos cuya marca de atraso puede cambiar
                _logger.LogInformation(
                    "Recuperando las cuotas de los prestamos activos evaluables a la fecha {Fecha}", today);

                //Vencida y no saldada -> debe quedar atrasada. Saldada y marcada -> se revierte la marca.
                var installments = await _loanInstallmentRepository.GetAllFindAsync(
                    i => i.Loan!.Status == LoanStatus.Activo
                         && ((i.paymentStatus != PaymentStatus.Pagada && i.DueDate < today)
                             || (i.paymentStatus == PaymentStatus.Pagada && i.IsOverdue)));

                _logger.LogInformation("Cuotas evaluadas en la corrida: {Cantidad}", installments.Count);
                #endregion

                #region aplicacion de la regla de atraso
                var toMark = installments
                    .Where(i => i.paymentStatus != PaymentStatus.Pagada && !i.IsOverdue)
                    .ToList();

                var toRevert = installments
                    .Where(i => i.paymentStatus == PaymentStatus.Pagada && i.IsOverdue)
                    .ToList();

                foreach (var installment in toMark) ApplyOverdueMark(installment, true, today);
                foreach (var installment in toRevert) ApplyOverdueMark(installment, false, today);

                var changed = toMark.Concat(toRevert).ToList();
                var summary = new OverdueInstallmentsResultDto
                {
                    ReviewedInstallments = installments.Count,
                    MarkedAsOverdue = toMark.Count,
                    OverdueMarkReverted = toRevert.Count,
                    AffectedLoans = changed.Select(i => i.LoanId).Distinct().Count()
                };
                #endregion

                if (changed.Count == 0)
                {
                    _logger.LogInformation("No existen cuotas cuyo indicador de atraso deba cambiar hoy.");
                    return ValidationResult<OverdueInstallmentsResultDto>.Success(summary);
                }

                #region persistencia de los cambios
                _logger.LogInformation(
                    "Intento de actualizacion del indicador de atraso de {Cantidad} cuotas de {Prestamos} prestamos",
                    changed.Count, summary.AffectedLoans);

                await _loanInstallmentRepository.UpdateRangeLoansInstallmentAsync(changed);
                var saved = await _loanInstallmentRepository.SaveChangesAsync();
                if (saved <= 0)
                {
                    _logger.LogError(
                        "Fallo al actualizar el indicador de atraso de las cuotas. Registros modificados {Registros}",
                        saved);
                    return ValidationResult<OverdueInstallmentsResultDto>.Failure(GeneralError.UnexpectedError);
                }
                #endregion

                _logger.LogInformation(
                    "Control de cuotas atrasadas completado. Marcadas {Marcadas}, revertidas {Revertidas}, prestamos afectados {Prestamos}",
                    summary.MarkedAsOverdue, summary.OverdueMarkReverted, summary.AffectedLoans);

                return ValidationResult<OverdueInstallmentsResultDto>.Success(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar el control automatico de cuotas atrasadas");
                return ValidationResult<OverdueInstallmentsResultDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        #region private methods
        private static void ApplyOverdueMark(LoanInstallment installment, bool isOverdue, DateTimeOffset today)
        {
            installment.IsOverdue = isOverdue;
            installment.ModifiedAt = today;
            installment.LastModifiedByIdUser = DomainConstants.SystemUserId;
        }
        #endregion
    }
}
