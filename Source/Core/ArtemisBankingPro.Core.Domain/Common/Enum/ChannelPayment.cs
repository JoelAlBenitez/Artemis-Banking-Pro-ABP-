namespace ArtemisBankingPro.Core.Domain.Common.Enum
{
    public enum ChannelPayment
    {
        Cliente = 1,
        Cajero = 2,

        Administrador = 3,

        //Pagos con tarjeta a favor de un comercio. Se agrega al final para no alterar los
        //valores ya persistidos por los demás módulos.
        HermesPay = 4
    }
}
