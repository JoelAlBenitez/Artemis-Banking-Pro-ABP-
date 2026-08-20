namespace ArtemisBankingPro.Core.Domain.Common.Enum
{
    public enum OperationType
    {
        AperturaCuenta = 1,
        Deposito = 2,
        Retiro = 3,
        TransferenciaEntreCuentas = 4,
        TransaccionExpress = 5,
        TransaccionBeneficiario = 6,
        PagoTarjeta = 7,
        PagoPrestamo = 8,
        AvanceEfectivo = 9,
        PagoHermesPay = 10,

        //Acreditación del capital aprobado de un préstamo en la cuenta principal del cliente.
        //Se agrega al final para no alterar los valores ya persistidos por los demás módulos.
        DesembolsoPrestamo = 11,

        //Transferencia del saldo remanente de una cuenta secundaria a la principal del cliente
        //al cancelarla. No es una transferencia del cliente: la origina el administrador, y
        //distinguirla permite excluirla de los indicadores de pagos del Dashboard.
        CancelacionCuenta = 12
    }
}
