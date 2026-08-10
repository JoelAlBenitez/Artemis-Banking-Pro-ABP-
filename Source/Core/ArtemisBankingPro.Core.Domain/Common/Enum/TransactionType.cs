namespace ArtemisBankingPro.Core.Domain.Common.Enum
{
    //DÉBITO: salida o disminución de fondos. CRÉDITO: ingreso o aumento de fondos.
    //La entidad Transaction pertenece al módulo Cliente; aquí solo se necesita el enum
    //para proyectar el historial de la cuenta en el detalle administrativo.
    public enum TransactionType
    {
        Debito = 1,
        Credito = 2
    }
}
