using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace Artemis_Banking_Pro.Core.Application.Exceptions
{
  
    public sealed class UnauthorizedException : ApplicationLayerException
    {
        private const string DefaultMessage = "Debe iniciar sesión para realizar esta acción.";

        public UnauthorizedException()
            : base(DefaultMessage)
        {
        }

        public UnauthorizedException(string message, IReadOnlyCollection<Error>? errors = null)
            : base(message, errors)
        {
        }
    }
}
