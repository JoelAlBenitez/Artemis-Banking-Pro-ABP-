using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace Artemis_Banking_Pro.Core.Application.Exceptions
{
    //Usuario autenticado sin permiso sobre el recurso. Se traduce a 403 Forbidden.
    //Cubre los rechazos que el filtro de autorización no puede resolver por sí solo:
    //rol no permitido en la API, auto-modificación de estado y comercio ajeno en Hermes Pay.
    public sealed class ForbiddenException : ApplicationLayerException
    {
        public ForbiddenException(Error error)
            : base(error.Description, new[] { error })
        {
        }

        public ForbiddenException(string message, IReadOnlyCollection<Error>? errors = null)
            : base(message, errors)
        {
        }
    }
}
