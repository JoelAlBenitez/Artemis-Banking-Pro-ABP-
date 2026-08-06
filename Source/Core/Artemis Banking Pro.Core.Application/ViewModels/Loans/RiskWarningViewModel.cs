using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Loans
{
    //Pantalla de advertencia de riesgo. Solo lectura: los datos del préstamo viajan para poder
    //reenviar la asignación cuando el administrador pulsa Confirmar asignación.
    public sealed class RiskWarningViewModel
    {
        public required string CustomerId { get; set; }
        public required TermMonths TermLoans { get; set; }
        public required decimal AmmountLoans { get; set; }
        public required decimal AnnualInterestRate { get; set; }

        public required string Message { get; set; }
        public required decimal CurrentDebt { get; set; }
        public required decimal ProjectedDebt { get; set; }
        public required decimal AverageDebt { get; set; }
    }
}
