using ArtemisBankingPro.Core.Domain.Common.Errors;

namespace Artemis_Banking_Pro.Core.Application.Exceptions
{
   
    public sealed class BusinessRuleException : ApplicationLayerException
    {
        public BusinessRuleException(Error error)
            : base(error.Description, new[] { error })
        {
        }

        public BusinessRuleException(string message, IReadOnlyCollection<Error>? errors = null)
            : base(message, errors)
        {
        }
    }
}
