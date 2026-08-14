using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace Artemis_Banking_Pro.Core.Application.Exceptions
{
    //Credenciales inválidas o cuenta inactiva. Se traduce a 401 Unauthorized.
    public sealed class UnauthorizedException : ApplicationLayerException
    {
        public UnauthorizedException(Error error)
            : base(error.Description, new[] { error })
        {
        }

        public UnauthorizedException(string message, IReadOnlyCollection<Error>? errors = null)
            : base(message, errors)
        {
        }
    }
}
