using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace Artemis_Banking_Pro.Core.Application.Exceptions
{
    
    public sealed class NotFoundException : ApplicationLayerException
    {
        public NotFoundException()
            : base(GeneralError.NonExistence.Description, new[] { GeneralError.NonExistence })
        {
        }

        public NotFoundException(Error error)
            : base(error.Description, new[] { error })
        {
        }

        public NotFoundException(string message, IReadOnlyCollection<Error>? errors = null)
            : base(message, errors)
        {
        }
    }
}
