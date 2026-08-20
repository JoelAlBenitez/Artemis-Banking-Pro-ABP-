namespace Artemis_Banking_Pro.Core.Application.DTOs.Base
{
    public abstract class BaseDto <TKey>
    {
        public TKey? Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
