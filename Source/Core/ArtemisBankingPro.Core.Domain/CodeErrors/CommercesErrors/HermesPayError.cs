using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace ArtemisBankingPro.Core.Domain.CodeErrors.CommercesErrors
{
    public static class HermesPayError
    {
        //Mensaje literal del documento funcional
        public static readonly Error AmountExceedsAvailableCredit
            = new("Error", "El monto de la transacción excede el crédito disponible de la tarjeta.");

        public static readonly Error NonExistsCreditCard
            = new("Error", "Los datos de la tarjeta no son válidos.");

        public static readonly Error CreditCardIsNotActive
            = new("Error", "La tarjeta se encuentra cancelada.");

        public static readonly Error CreditCardExpired
            = new("Error", "La tarjeta se encuentra vencida.");

        //El CVC y la expiración se validan juntos: distinguirlos ayudaría a adivinar los datos
        public static readonly Error InvalidCardCredentials
            = new("Error", "Los datos de la tarjeta no son válidos.");

        public static readonly Error CommerceWithoutActivePrimaryAccount
            = new("Error", "El comercio no tiene una cuenta de ahorro principal activa para recibir el pago.");

        public static readonly Error CommerceNotOwnedByUser
            = new("Error", "Acceso denegado. No tiene permisos para utilizar este recurso.");

        public static readonly Error FailedProcessPayment
            = new("Error", "Ha ocurrido un error imprevisto al procesar el pago. Favor intente de nuevo más tarde.");

        public static readonly Error PaymentProcessedWithoutNotification
            = new("Advertencia", "El pago fue procesado correctamente, pero no fue posible enviar una o más notificaciones por correo.");
    }
}
