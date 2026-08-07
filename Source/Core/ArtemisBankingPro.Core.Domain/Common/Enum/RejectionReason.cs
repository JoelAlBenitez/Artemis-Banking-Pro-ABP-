namespace ArtemisBankingPro.Core.Domain.Common.Enum
{
    //Solo se declaran los motivos que aplican a tarjetas de crédito. Los valores 1, 2, 6, 7 y 8
    //quedan reservados para los motivos de cuentas y transacciones de las demás funcionalidades.
    public enum RejectionReason
    {
        TarjetaCancelada = 3,
        TarjetaVencida = 4,
        CreditoInsuficiente = 5
    }
}
