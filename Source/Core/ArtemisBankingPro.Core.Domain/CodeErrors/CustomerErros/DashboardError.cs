using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros
{
    public static class DashboardError
    {
        public static readonly Error ProductNotFound =
            new("Error", "El producto financiero seleccionado no existe o no pertenece al cliente.");

        public static readonly Error AccountNotFound =
            new("Error", "La cuenta de ahorros seleccionada no existe o no pertenece al cliente.");

        public static readonly Error CardNotFound =
            new("Error", "La tarjeta de crédito seleccionada no existe o no pertenece al cliente.");

        public static readonly Error LoanNotFound =
            new("Error", "El préstamo seleccionado no existe o no pertenece al cliente.");

        public static readonly Error UnauthorizedAccess =
            new("Error", "No tiene permisos para acceder a la información de este producto.");
    }
}
