using Artemis_Banking_Pro.Core.Application.ViewModels.Base;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.CreditCards
{
    public sealed class CreditCardViewModel : BaseViewModel<int>
    {
        public required string MaskedCardNumber { get; set; }
        public required string FullNameCustomer { get; set; }
        public required decimal CreditLimit { get; set; }
        public required string ExpirationDate { get; set; }
        public required decimal OwedAmount { get; set; }
        //Etiqueta de presentación: Activa / Cancelada
        public required string StateCreditCard { get; set; }
    }
}
