namespace Artemis_Banking_Pro.Core.Application.DTOs.Loans
{
    //Paso 1 de la asignación: clientes activos sin préstamo activo y el monto promedio de deuda
    //de los clientes activos del sistema, mostrado en la parte superior de la pantalla.
    public sealed class ClientsForLoanAssignmentDto
    {
        public required decimal AverageDebt { get; set; }
        public required IReadOnlyCollection<ClientLoansDto> Clients { get; set; }
    }
}
