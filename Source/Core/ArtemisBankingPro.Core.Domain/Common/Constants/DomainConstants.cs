namespace ArtemisBankingPro.Core.Domain.Common.Constants
{
    public static class DomainConstants
    {
        //Paginación obligatoria en todos los listados administrativos
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 20;

        //Número de préstamo: 9 dígitos como texto, no repetible como préstamo ni como cuenta
        public const int LoanNumberLength = 9;

        //Precisión decimal de todo valor monetario
        public const int MoneyPrecision = 18;
        public const int MoneyScale = 2;

        //Precisión de la tasa de interés anual
        public const int RatePrecision = 9;
        public const int RateScale = 4;

        //Largo del identificador de usuario de Identity referenciado sin FK física
        //esto esta asi para ser usado en las configuraciones puesto que identity genera un guid.toString()
        // se debe tener exactamente la longitud exacta de dicho id pq sino dara error en la asignacion de dicho id
        public const int IdentityUserIdLength = 450;

        //Autor de auditoría de los procesos automáticos del sistema (no hay usuario autenticado)
        public const string SystemUserId = "SYSTEM";
    }
}
