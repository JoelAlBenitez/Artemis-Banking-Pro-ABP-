using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace Artemis_Banking_Pro.Core.Application.Common
{
    //Los filtros de la Web API llegan como texto con los valores literales del documento
    //funcional («activa», «cancelada», «todas»…). Se traducen aquí para que los validadores y
    //los handlers compartan una sola definición de lo que es válido.
    public static class ApiFilterValues
    {
        private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

        public static class Commerce
        {
            public const string Active = "activo";
            public const string Inactive = "inactivo";
            public const string All = "todos";

            public static readonly string[] Allowed = [Active, Inactive, All];

            public static bool IsAllowed(string? value)
                => value is null || Allowed.Contains(value, Comparer);

            //null significa «sin filtro de estado»: solo lo produce el valor «todos»
            public static CommerceStatus? ToStatus(string? value)
                => Comparer.Equals(value, All) ? null
                 : Comparer.Equals(value, Inactive) ? CommerceStatus.Inactivo
                 : CommerceStatus.Activo;
        }

        public static class SavingsAccount
        {
            public const string Active = "activa";
            public const string Cancelled = "cancelada";
            public const string All = "todas";

            public static readonly string[] AllowedStatus = [Active, Cancelled, All];

            public const string Primary = "principal";
            public const string Secondary = "secundaria";

            public static readonly string[] AllowedTypes = [Primary, Secondary, All];

            public static bool IsAllowedStatus(string? value)
                => value is null || AllowedStatus.Contains(value, Comparer);

            public static bool IsAllowedType(string? value)
                => value is null || AllowedTypes.Contains(value, Comparer);

            public static SavingsAccountStatusFilter ToStatusFilter(string? value)
                => Comparer.Equals(value, All) ? SavingsAccountStatusFilter.Todas
                 : Comparer.Equals(value, Cancelled) ? SavingsAccountStatusFilter.Canceladas
                 : SavingsAccountStatusFilter.Activas;

            public static SavingsAccountTypeFilter ToTypeFilter(string? value)
                => Comparer.Equals(value, Primary) ? SavingsAccountTypeFilter.Principal
                 : Comparer.Equals(value, Secondary) ? SavingsAccountTypeFilter.Secundaria
                 : SavingsAccountTypeFilter.Todas;
        }

        public static class CreditCard
        {
            public const string Active = "activa";
            public const string Cancelled = "cancelada";
            public const string All = "todas";

            public static readonly string[] Allowed = [Active, Cancelled, All];

            public static bool IsAllowed(string? value)
                => value is null || Allowed.Contains(value, Comparer);

            public static CreditCardStatusFilter ToStatusFilter(string? value)
                => Comparer.Equals(value, All) ? CreditCardStatusFilter.Todas
                 : Comparer.Equals(value, Cancelled) ? CreditCardStatusFilter.Canceladas
                 : CreditCardStatusFilter.Activas;
        }

        public static class Loan
        {
            public const string Active = "activos";
            public const string Completed = "completados";
            public const string All = "todos";

            public static readonly string[] Allowed = [Active, Completed, All];

            public static bool IsAllowed(string? value)
                => value is null || Allowed.Contains(value, Comparer);

            public static LoanStatusFilter ToStatusFilter(string? value)
                => Comparer.Equals(value, All) ? LoanStatusFilter.Todos
                 : Comparer.Equals(value, Completed) ? LoanStatusFilter.Completados
                 : LoanStatusFilter.Activos;
        }

        public static class User
        {
            //El rol Comercio no se admite: tiene su propio endpoint de listado y de creación
            public static readonly string[] AllowedRoles =
            [
                nameof(Roles.Administrador),
                nameof(Roles.Cajero),
                nameof(Roles.Cliente)
            ];

            public static bool IsAllowedRole(string? value)
                => value is null || AllowedRoles.Contains(value, Comparer);

            public static Roles? ToRole(string? value)
                => value is null ? null
                 : System.Enum.TryParse<Roles>(value, ignoreCase: true, out var role) ? role
                 : null;
        }
    }
}
