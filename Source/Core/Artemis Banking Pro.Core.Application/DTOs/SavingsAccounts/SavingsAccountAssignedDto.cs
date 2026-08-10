namespace Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts
{
    //Datos de la cuenta recién asignada usados por la notificación al cliente.
    public sealed class SavingsAccountAssignedDto
    {
        public required string CustomerId { get; set; }
        public required string AccountNumber { get; set; }
        public required decimal InitialBalance { get; set; }
        public required DateTimeOffset AssignedAt { get; set; }
    }
}
