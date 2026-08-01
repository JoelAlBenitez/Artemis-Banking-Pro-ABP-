using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace ArtemisBankingPro.Core.Domain.CodeErrors.CreditCardsErrors
{
    public static class CreditCardError
    {
        public static readonly Error NonExistsCustomerByIdCard
            = new("Error", "No existe un cliente registrado con esta cédula.");

        public static readonly Error NonExistsCreditCards
            = new("Error", "Este cliente no tiene tarjetas de crédito registradas.");

        public static readonly Error CustomerRequired
            = new("Error", "Debe seleccionar un cliente para continuar.");

        public static readonly Error CustomerIsNotActive
            = new("Error", "Solo se puede asignar tarjetas de crédito a clientes activos.");

        public static readonly Error InvalidCreditLimitAssignment
            = new("Error", "El límite de crédito debe ser mayor que cero.");

        public static readonly Error NonExistsCreditCard
            = new("Error", "La tarjeta seleccionada no existe.");

        public static readonly Error CreditCardIsCancelled
            = new("Error", "No se puede modificar una tarjeta cancelada.");

        public static readonly Error InvalidCreditLimit
            = new("Error", "El límite de la tarjeta debe ser mayor que cero.");

        public static readonly Error CreditLimitLowerThanOwedAmount
            = new("Error", "El límite de la tarjeta no puede ser inferior al monto adeudado actualmente.");

        public static readonly Error CreditCardWithPendingDebt
            = new("Error", "Para cancelar esta tarjeta, el cliente debe saldar la totalidad de la deuda pendiente.");

        public static readonly Error FailedGenerateCardNumber
            = new("Error", "No fue posible generar el número de la tarjeta. Favor intente de nuevo más tarde.");

        public static readonly Error FailedProcessCreditCard
            = new("Error", "Ha ocurrido un error imprevisto al procesar la tarjeta de crédito. Favor intente de nuevo más tarde.");

        public static readonly Error CreditCardCreatedWithoutNotification
            = new("Advertencia", "La tarjeta fue creada correctamente, pero no fue posible enviar el correo de notificación.");

        public static readonly Error CreditLimitUpdatedWithoutNotification
            = new("Advertencia", "El límite fue actualizado correctamente, pero no fue posible enviar el correo de notificación.");

        public static readonly Error AdminUserRequired
            = new("Error", "No fue posible identificar al administrador responsable de la operación.");
    }
}
