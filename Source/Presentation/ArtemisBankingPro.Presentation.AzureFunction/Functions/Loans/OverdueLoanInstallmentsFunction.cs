using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ArtemisBankingPro.Presentation.AzureFunction.Functions.Loans
{
    /// Proceso automático diario de control de cuotas atrasadas.
    /// Revisa las cuotas pendientes de todos los préstamos activos y actualiza su indicador de atraso.
    public sealed class OverdueLoanInstallmentsFunction
    {
        private readonly ILoansOverdueServices _loansOverdueServices;
        private readonly ILogger<OverdueLoanInstallmentsFunction> _logger;

        public OverdueLoanInstallmentsFunction(
            ILoansOverdueServices loansOverdueServices,
            ILogger<OverdueLoanInstallmentsFunction> logger)
        {
            _loansOverdueServices = loansOverdueServices;
            _logger = logger;
        }

        [Function("OverdueLoanInstallments")]
        public async Task Run([TimerTrigger("%LoansOverdueScheduleCron%")] TimerInfo timer)
        {
            _logger.LogInformation(
                "Inicio del control automatico de cuotas atrasadas a las {Inicio}", DateTimeOffset.UtcNow);

            if (timer.IsPastDue)
            {
                _logger.LogWarning("La corrida programada del control de cuotas atrasadas se ejecuta con retraso.");
            }

            if (timer.ScheduleStatus is not null)
            {
                _logger.LogInformation(
                    "Proxima corrida programada para {ProximaCorrida}", timer.ScheduleStatus.Next);
            }

            var result = await _loansOverdueServices.ReviewOverdueInstallmentsAsync();

            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    _logger.LogError(
                        "El control automatico de cuotas atrasadas finalizo con error {Codigo}: {Descripcion}",
                        error.Code, error.Description);
                }

                return;
            }

            var summary = result.Value!;
            _logger.LogInformation(
                "Control automatico de cuotas atrasadas finalizado. Cuotas evaluadas {Evaluadas}, " +
                "marcadas como atrasadas {Marcadas}, marca revertida {Revertidas}, prestamos afectados {Prestamos}",
                summary.ReviewedInstallments,
                summary.MarkedAsOverdue,
                summary.OverdueMarkReverted,
                summary.AffectedLoans);
        }
    }
}
