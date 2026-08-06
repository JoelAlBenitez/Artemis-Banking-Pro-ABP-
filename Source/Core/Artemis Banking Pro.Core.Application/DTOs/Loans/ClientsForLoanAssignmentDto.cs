namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
   
    public sealed class ClientsForLoanAssignmentDto
    {
        public required decimal AverageDebt { get; set; }
        public required IReadOnlyCollection<ClientLoansDto> Clients { get; set; }
    }
}
