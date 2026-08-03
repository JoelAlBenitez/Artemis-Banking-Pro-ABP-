namespace ArtemisBankingPro.Core.Domain.Common.Enum
{
    //La principal se crea automáticamente al registrar el cliente; desde el módulo
    //administrador solo se asignan cuentas secundarias.
    public enum SavingsAccountType
    {
        Principal = 1,
        Secundaria = 2
    }
}
