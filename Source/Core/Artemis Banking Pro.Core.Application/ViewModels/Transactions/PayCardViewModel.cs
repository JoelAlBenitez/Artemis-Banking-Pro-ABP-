using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Transactions
{
    public class PayCardViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar la cuenta de ahorro de origen.")]
        public required string SourceAccountNumber { get; set; }

        [Required(ErrorMessage = "Debe seleccionar la tarjeta de crédito destino.")]
        public int CreditCardId { get; set; }

        [Required(ErrorMessage = "El monto a pagar es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
        public decimal Amount { get; set; }
    }
}
