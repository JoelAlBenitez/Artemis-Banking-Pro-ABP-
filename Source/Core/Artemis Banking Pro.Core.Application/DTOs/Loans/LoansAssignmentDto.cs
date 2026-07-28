using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    public sealed class LoansAssignmentDto
    {
        public required int CustomerId { get; set; }
        public required int TermLoans { get; set; }
        public required decimal AmmountLoans { get; set; }
        public required decimal AnnualInterestRate { get; set; }
    }
}
