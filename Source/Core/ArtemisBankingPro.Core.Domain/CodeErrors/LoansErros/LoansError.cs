using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace ArtemisBankingPro.Core.Domain.CodeErrors.LoansErros
{
    public static class LoansError
    {
        public static readonly Error NonExistsLoans
            = new("Error", "Este cliente no tiene préstamos registrados.");

        public static readonly Error CustomerWithLoanExist =
            new("Error", "Este cliente ya tiene un préstamo activo asignado.");

        public static readonly Error InvalidTerm
            = new("Error", "El plazo seleccionado no es válido.");

        public static readonly Error FailedProcessLoan
            = new("Error", "Ha ocurrido un error imprevisto al procesar el prestamo. Favor intente de nuevo más tarde.");

        public static readonly Error FaildGenerateLoansInstallment
            = new("Error", "La tabla de amortización de este prestamo no pudo ser creada. Favor intente de nuevo más tarde.");

        public static readonly Error NonExistAccountFirstActive
            = new("Error", "El cliente no tiene una cuenta de ahorro principal activa para recibir el desembolso del préstamo.");

        public static readonly Error NonExistsCustomerByIdCard
            = new("Error", "No existe un cliente registrado con esta cédula.");

        public static readonly Error NonExistsLoan
            = new("Error", "El préstamo seleccionado no existe.");

        public static readonly Error LoanIsNotActive
            = new("Error", "Solo se puede modificar la tasa de interés de préstamos activos.");

        public static readonly Error NegativeAnnualInterestRate
            = new("Error", "La tasa de interés anual no puede ser negativa.");

        public static readonly Error NonExistsFutureInstallments
            = new("Error", "No existen cuotas futuras pendientes para recalcular.");

        public static readonly Error RateUpdatedWithoutNotification
            = new("Advertencia", "La tasa fue modificada correctamente, pero no fue posible enviar el correo de notificación.");

        public static readonly Error NonExistLoansByIndicateState
            = new("Advertencia", "No existen prestamos para el estado indicado.");

        //Literales de las páginas 34-43 que faltaban por centralizar

        public static readonly Error NonSelectedCustomer
            = new("Error", "Debe seleccionar un cliente para continuar.");

        public static readonly Error InvalidAmount
            = new("Error", "El monto a prestar debe ser mayor que cero.");

        public static readonly Error CustomerWithCurrentHighRisk
            = new("Advertencia", "Este cliente se considera de alto riesgo, ya que su deuda actual supera el promedio del sistema.");

        public static readonly Error CustomerWithProjectedHighRisk
            = new("Advertencia", "Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, ya que su deuda superará el umbral promedio del sistema.");

        public static readonly Error LoanCreatedWithoutNotification
            = new("Advertencia", "El préstamo fue creado correctamente, pero no fue posible enviar el correo de notificación.");

        //Casos que el documento funcional no cubre, construidos con el mismo patrón

        public static readonly Error FailedGenerateLoanNumber
            = new("Error", "El número del préstamo no pudo ser generado. Favor intente de nuevo más tarde.");

        public static readonly Error CustomerIsNotActive
            = new("Error", "Solo se pueden asignar préstamos a clientes activos.");

        public static readonly Error AdminNotIdentified
            = new("Error", "No fue posible identificar al administrador responsable de la asignación.");
    }
}
