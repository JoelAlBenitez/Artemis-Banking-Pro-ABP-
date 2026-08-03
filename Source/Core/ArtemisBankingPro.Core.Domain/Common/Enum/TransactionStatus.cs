namespace ArtemisBankingPro.Core.Domain.Common.Enum
{
    //Los intentos rechazados se conservan como evidencia y no afectan los balances.
    public enum TransactionStatus
    {
        Aprobada = 1,
        Rechazada = 2
    }
}
