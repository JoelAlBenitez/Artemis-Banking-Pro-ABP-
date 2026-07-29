using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    public sealed class LoansAssignmentDto
    {
        //Identificador del cliente en Identity: texto, sin FK física
        public required string CustomerId { get; set; }
        public required TermMonths TermLoans { get; set; }
        public required decimal AmmountLoans { get; set; }
        public required decimal AnnualInterestRate { get; set; }
    }
}
