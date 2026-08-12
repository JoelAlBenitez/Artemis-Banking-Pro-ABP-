namespace ArtemisBankingPro.Core.Domain.Common.Enum
{
    //Un comercio inactivo no procesa pagos por Hermes Pay. La baja es un cambio de estado:
    //el comercio nunca se elimina físicamente.
    public enum CommerceStatus
    {
        Activo = 1,
        Inactivo = 2
    }
}
