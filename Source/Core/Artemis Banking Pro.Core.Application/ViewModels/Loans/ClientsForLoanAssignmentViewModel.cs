namespace Artemis_Banking_Pro.Core.Application.ViewModels.Loans
{
    //Paso 1 de la asignación: monto promedio de deuda arriba, listado de clientes activos sin
    //préstamo activo y búsqueda por cédula.
    public sealed class ClientsForLoanAssignmentViewModel
    {
        public required decimal AverageDebt { get; set; }
        public required IReadOnlyCollection<ClientLoansViewModel> Clients { get; set; }
        public string? IdCard { get; set; }
    }
}
