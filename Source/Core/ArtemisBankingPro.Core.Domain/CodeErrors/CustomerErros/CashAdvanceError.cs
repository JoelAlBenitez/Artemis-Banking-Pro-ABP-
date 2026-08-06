using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros
{
    public static class CashAdvanceError
    {
        public static readonly Error CardNotActive = new(
            "CashAdvance.CardNotActive",
            "La tarjeta seleccionada no se encuentra activa."
        );

        public static readonly Error CardExpired = new(
            "CashAdvance.CardExpired",
            "La tarjeta seleccionada se encuentra vencida."
        );

        public static readonly Error AccountNotActive = new(
            "CashAdvance.AccountNotActive",
            "La cuenta de ahorro seleccionada no se encuentra activa."
        );

        public static readonly Error AmountInvalid = new(
            "CashAdvance.AmountInvalid",
            "El monto del avance debe ser mayor que cero."
        );

        public static readonly Error InsufficientCredit = new(
            "CashAdvance.InsufficientCredit",
            "El avance solicitado excede el crédito disponible de la tarjeta seleccionada."
        );
    }
}
