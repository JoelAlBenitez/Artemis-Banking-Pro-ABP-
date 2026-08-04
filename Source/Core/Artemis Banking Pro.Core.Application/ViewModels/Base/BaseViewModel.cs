namespace Artemis_Banking_Pro.Core.Application.ViewModels.Base
{
    public abstract class BaseViewModel<TKey>
    {
        public TKey? Id { get; set; }
    }
}
