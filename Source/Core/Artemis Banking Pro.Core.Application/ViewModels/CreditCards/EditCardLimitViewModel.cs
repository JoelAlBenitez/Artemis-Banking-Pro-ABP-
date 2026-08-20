using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.CreditCards
{
    public sealed class EditCardLimitViewModel
    {
        [Required(ErrorMessage = "Debe indicar una tarjeta valida.")]
        public required int CreditCardId { get; set; }

        [Required(ErrorMessage = "El límite de la tarjeta es requerido.")]
        [Range(0.01, 999999999999.99, ErrorMessage = "El límite de la tarjeta debe ser mayor que cero.")]
        public required decimal CreditLimit { get; set; }
    }
}
