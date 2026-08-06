using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.CreditCards
{
    public sealed class CancelCreditCardViewModel
    {
        [Required(ErrorMessage = "Debe indicar una tarjeta valida.")]
        public required int CreditCardId { get; set; }

        public required string LastFourDigits { get; set; }

        public string ConfirmationMessage => $"¿Está seguro que desea cancelar la tarjeta {LastFourDigits}?";
    }
}
