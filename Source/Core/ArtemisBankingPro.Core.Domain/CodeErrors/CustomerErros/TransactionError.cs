using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros
{
    public static class TransactionError
    {
        public static readonly Error InvalidAmount = new(
            "Transaction.InvalidAmount", 
            "El monto ingresado debe ser mayor que cero."
        );

        public static readonly Error InsufficientFunds = new(
            "Transaction.InsufficientFunds", 
            "La cuenta de ahorros de origen no posee fondos suficientes."
        );

        public static readonly Error OriginAccountNotFound = new(
            "Transaction.OriginAccountNotFound", 
            "La cuenta de ahorros origen no existe o no pertenece al cliente."
        );

        public static readonly Error DestinationAccountNotFound = new(
            "Transaction.DestinationAccountNotFound", 
            "La cuenta de ahorros destino no existe."
        );

        public static readonly Error DestinationAccountCanceled = new(
            "Transaction.DestinationAccountCanceled", 
            "La cuenta de ahorros destino se encuentra cancelada."
        );

        public static readonly Error SameAccountTransfer = new(
            "Transaction.SameAccountTransfer", 
            "No se puede realizar una transferencia a la misma cuenta de origen."
        );

        public static readonly Error CreditCardNotFound = new(
            "Transaction.CreditCardNotFound", 
            "La tarjeta de crédito seleccionada no existe o no pertenece al cliente."
        );

        public static readonly Error CreditCardCanceled = new(
            "Transaction.CreditCardCanceled", 
            "La tarjeta de crédito seleccionada se encuentra cancelada."
        );

        public static readonly Error CreditCardOverpayment = new(
            "Transaction.CreditCardOverpayment", 
            "El monto de pago no puede exceder el balance adeudado actual de la tarjeta."
        );

        public static readonly Error LoanNotFound = new(
            "Transaction.LoanNotFound", 
            "El préstamo seleccionado no existe o no pertenece al cliente."
        );

        public static readonly Error NoPendingInstallments = new(
            "Transaction.NoPendingInstallments", 
            "El préstamo seleccionado no posee cuotas pendientes por pagar."
        );

        public static readonly Error LoanOverpayment = new(
            "Transaction.LoanOverpayment", 
            "El monto de pago no puede exceder el balance pendiente del préstamo."
        );

        public static readonly Error BeneficiaryNotFound = new(
            "Transaction.BeneficiaryNotFound", 
            "El beneficiario seleccionado no está registrado o no se encuentra activo."
        );


        public static readonly Error MinTwoAccountsRequired = new(
            "Transaction.MinTwoAccountsRequired", 
            "Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas."
        );

        public static readonly Error TransferSameAccount = new(
            "Transaction.TransferSameAccount", 
            "La cuenta de origen y la cuenta de destino no pueden ser la misma."
        );

        public static readonly Error TransferInvalidAmount = new(
            "Transaction.TransferInvalidAmount", 
            "El monto a transferir debe ser mayor que cero."
        );

        public static readonly Error TransferInsufficientFunds = new(
            "Transaction.TransferInsufficientFunds", 
            "No dispone del monto requerido en la cuenta seleccionada."
        );
    }
}
