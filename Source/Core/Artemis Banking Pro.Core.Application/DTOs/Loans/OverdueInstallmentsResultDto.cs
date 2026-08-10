namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    //Resumen del proceso automático diario de control de cuotas atrasadas.
    public sealed class OverdueInstallmentsResultDto
    {
        //Cuotas de préstamos activos evaluadas en la corrida
        public required int ReviewedInstallments { get; set; }

        //Cuotas vencidas y no pagadas completamente que quedaron marcadas como atrasadas
        public required int MarkedAsOverdue { get; set; }

        //Cuotas atrasadas que fueron pagadas y a las que se les revirtió la marca
        public required int OverdueMarkReverted { get; set; }

        //Préstamos distintos afectados por la corrida
        public required int AffectedLoans { get; set; }
    }
}
