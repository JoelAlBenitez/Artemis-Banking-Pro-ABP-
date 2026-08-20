using ArtemisBankingPro.Core.Domain.Common.Constants;
using FluentValidation;

namespace Artemis_Banking_Pro.Core.Application.Common
{
    //Las tres reglas de paginación se repiten en todos los listados de la Web API. Se declaran
    //una sola vez para que un listado nuevo no pueda quedar con otro límite por descuido.
    public static class PaginationRules
    {
        public static IRuleBuilderOptions<T, int> ValidPage<T>(this IRuleBuilder<T, int> rule)
            => rule.GreaterThan(0)
                   .WithMessage("El número de página debe ser mayor que cero.");

        public static IRuleBuilderOptions<T, int> ValidPageSize<T>(this IRuleBuilder<T, int> rule)
            => rule.GreaterThan(0)
                   .WithMessage("La cantidad de registros por página debe ser mayor que cero.")
                   .LessThanOrEqualTo(DomainConstants.MaxPageSize)
                   .WithMessage($"La cantidad máxima de registros por página es {DomainConstants.MaxPageSize}.");

        //Los endpoints por identificador comparten la misma regla estructural: la ruta acepta
        //cualquier entero, pero cero o negativo nunca puede corresponder a un registro.
        public static IRuleBuilderOptions<T, int> ValidIdentifier<T>(this IRuleBuilder<T, int> rule)
            => rule.GreaterThan(0)
                   .WithMessage("El identificador debe ser mayor que cero.");

        public static IRuleBuilderOptions<T, string> ValidIdentifier<T>(this IRuleBuilder<T, string> rule)
            => rule.NotEmpty()
                   .WithMessage("El identificador es requerido.");
    }
}
