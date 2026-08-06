using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    public sealed class LoansAssignmentDto
    {
        public required string CustomerId { get; set; }
        public required TermMonths TermLoans { get; set; }
        public required decimal AmmountLoans { get; set; }
        public required decimal AnnualInterestRate { get; set; }

        //El administrador confirma la asignación aunque el cliente sea de alto riesgo. En la
        //WebApp lo activa la pantalla de advertencia; en la Web API es el campo confirmHighRisk.
        public bool ConfirmHighRisk { get; set; }
    }
}
