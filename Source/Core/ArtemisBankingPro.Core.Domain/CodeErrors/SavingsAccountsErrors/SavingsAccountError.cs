using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace ArtemisBankingPro.Core.Domain.CodeErrors.SavingsAccountsErrors
{
    public static class SavingsAccountError
    {
        #region mensajes literales del documento funcional

        //Búsqueda por cédula en el listado principal
        public static readonly Error NonExistsCustomerByIdCard
            = new("Error", "No existe un cliente registrado con esta cédula.");

        public static readonly Error NonExistsSavingsAccounts
            = new("Error", "Este cliente no tiene cuentas de ahorro registradas.");

        //Selección de cliente para asignar una cuenta secundaria
        public static readonly Error CustomerRequired
            = new("Error", "Debe seleccionar un cliente para continuar.");

        public static readonly Error CustomerIsNotActive
            = new("Error", "Solo se puede asignar cuentas de ahorro a clientes activos.");

        public static readonly Error CustomerWithoutActivePrimaryAccount
            = new("Error", "El cliente debe tener una cuenta de ahorro principal activa antes de asignarle una cuenta secundaria.");

        //Formulario de asignación
        public static readonly Error NegativeInitialBalance
            = new("Error", "El balance inicial no puede ser negativo.");

        //Cancelación de una cuenta secundaria
        public static readonly Error NonExistsSavingsAccount
            = new("Error", "La cuenta seleccionada no existe.");

        public static readonly Error SavingsAccountAlreadyCancelled
            = new("Error", "La cuenta seleccionada ya se encuentra cancelada.");

        public static readonly Error PrimaryAccountCannotBeCancelled
            = new("Error", "Las cuentas principales no pueden ser canceladas.");

        public static readonly Error WithoutPrimaryAccountToReceiveFunds
            = new("Error", "No es posible cancelar la cuenta porque el cliente no tiene una cuenta principal activa para recibir los fondos.");

        #endregion

        #region mensajes construidos con el mismo patrón del documento

        //El documento exige la validación «El balance inicial es requerido» pero no fija su
        //texto de error; se redacta siguiendo la redacción de los demás campos requeridos.
        public static readonly Error InitialBalanceRequired
            = new("Error", "El balance inicial es requerido.");

        //Equivalente a CreditCardError.FailedGenerateCardNumber para el número de 9 dígitos
        public static readonly Error FailedGenerateAccountNumber
            = new("Error", "No fue posible generar el número de la cuenta de ahorro. Favor intente de nuevo más tarde.");

        public static readonly Error FailedProcessSavingsAccount
            = new("Error", "Ha ocurrido un error imprevisto al procesar la cuenta de ahorro. Favor intente de nuevo más tarde.");

        public static readonly Error AdminUserRequired
            = new("Error", "No fue posible identificar al administrador responsable de la operación.");

        //Un fallo de correo nunca revierte la operación: se informa como advertencia
        public static readonly Error SavingsAccountCreatedWithoutNotification
            = new("Advertencia", "La cuenta de ahorro fue creada correctamente, pero no fue posible enviar el correo de notificación.");

        public static readonly Error SavingsAccountCancelledWithoutNotification
            = new("Advertencia", "La cuenta de ahorro fue cancelada correctamente, pero no fue posible enviar el correo de notificación.");

        #endregion
    }
}
