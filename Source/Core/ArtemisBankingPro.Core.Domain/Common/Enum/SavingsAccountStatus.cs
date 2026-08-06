namespace ArtemisBankingPro.Core.Domain.Common.Enum
{
    //La baja de una cuenta es un cambio de estado: nunca se elimina físicamente.
    public enum SavingsAccountStatus
    {
        Activa = 1,
        Cancelada = 2
    }
}
