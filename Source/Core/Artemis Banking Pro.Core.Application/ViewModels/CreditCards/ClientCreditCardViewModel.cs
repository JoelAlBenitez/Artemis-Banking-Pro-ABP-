using Artemis_Banking_Pro.Core.Application.ViewModels.Base;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.CreditCards
{
    public sealed class ClientCreditCardViewModel : BaseViewModel<string>
    {
        public required string IdCard { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required decimal TotalDebtAmount { get; set; }
    }
}
