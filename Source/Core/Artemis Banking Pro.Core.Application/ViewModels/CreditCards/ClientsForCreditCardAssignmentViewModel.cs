namespace Artemis_Banking_Pro.Core.Application.ViewModels.CreditCards
{
    //Paso 1 de la asignación: monto promedio de deuda arriba, listado de clientes activos y
    //búsqueda por cédula. Un cliente puede tener varias tarjetas, así que no se descarta ninguno.
    public sealed class ClientsForCreditCardAssignmentViewModel
    {
        public required decimal AverageDebt { get; set; }
        public required IReadOnlyCollection<ClientCreditCardViewModel> Clients { get; set; }
        public string? IdCard { get; set; }
    }
}
