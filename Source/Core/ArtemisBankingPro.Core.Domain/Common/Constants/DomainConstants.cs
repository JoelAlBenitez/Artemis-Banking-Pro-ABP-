namespace ArtemisBankingPro.Core.Domain.Common.Constants
{
    public static class DomainConstants
    {
        //Paginación obligatoria en todos los listados administrativos
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 20;

        //Número de préstamo: 9 dígitos como texto, no repetible como préstamo ni como cuenta
        public const int LoanNumberLength = 9;

        //Número de cuenta de ahorro: 9 dígitos como texto, comparte la unicidad con los préstamos
        public const int AccountNumberLength = 9;

        //Número de tarjeta: 16 dígitos como texto, único en el sistema
        public const int CardNumberLength = 16;

        //Copia desnormalizada usada en listados, correos y logs
        public const int LastFourDigitsLength = 4;

        //CVC de 3 dígitos persistido como hash SHA-256 de 64 caracteres hexadecimales
        public const int CvcLength = 3;
        public const int CvcHashLength = 64;

        //Expiración de la tarjeta: fecha actual + 3 años
        public const int CardExpirationYears = 3;

        //Nombre del comercio mostrado en el historial de consumos
        public const int CommerceNameLength = 120;

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
