namespace ArtemisBankingPro.Core.Domain.Common.Enum
{
    public enum ChannelPayment
    {
        Cliente = 1,
        Cajero = 2,

        //Origen administrativo: el desembolso de un préstamo lo ejecuta un administrador, no el
        //cliente ni el cajero. Se agrega al final para no alterar los valores ya persistidos.
        Administrador = 3
    }
}
