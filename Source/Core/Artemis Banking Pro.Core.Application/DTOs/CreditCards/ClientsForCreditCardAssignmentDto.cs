namespace Artemis_Banking_Pro.Core.Application.DTOs.CreditCards
{
    public sealed class ClientsForCreditCardAssignmentDto
    {
        public required decimal AverageDebt { get; set; }
        public required IReadOnlyCollection<ClientCreditCardDto> Clients { get; set; }
    }
}
