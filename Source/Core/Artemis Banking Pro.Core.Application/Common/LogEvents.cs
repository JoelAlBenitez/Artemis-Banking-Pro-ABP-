namespace Artemis_Banking_Pro.Core.Application.Common
{
    
    public static class LogEvents
    {
        public static class Operations
        {
            public const string Deposit = "Deposito";
            public const string Withdrawal = "Retiro";
            public const string Transfer = "Transferencia";
            public const string CreditCardPayment = "PagoTarjeta";
            public const string LoanPayment = "PagoPrestamo";
            public const string CashAdvance = "AvanceEfectivo";
            public const string HermesPayPayment = "PagoHermesPay";
            public const string LoanCreation = "CreacionPrestamo";
            public const string CreditCardAssignment = "AsignacionTarjeta";
            public const string FinancialProductCancellation = "CancelacionProductoFinanciero";
        }

        public static class Results
        {
            public const string Approved = "Aprobada";
            public const string Rejected = "Rechazada";
            public const string Failed = "Error";
        }

        
        public static class Templates
        {
            public const string FinancialOperation =
                "Operación financiera {Operacion} con resultado {Resultado}. Detalle: {@Detalle}";

            public const string EmailNotificationFailed =
                "No fue posible enviar el correo de notificación de la operación {Operacion}. " +
                "La operación no se revierte. Detalle: {@Detalle}";

            public const string UnhandledError =
                "Error no controlado al ejecutar {Accion}. Resultado: {Resultado}";
        }
    }
}
