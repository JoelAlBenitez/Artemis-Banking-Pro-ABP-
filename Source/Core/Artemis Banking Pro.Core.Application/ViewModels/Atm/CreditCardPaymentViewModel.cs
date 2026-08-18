using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class CreditCardPaymentViewModel
    {
        [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
        public string SourceAccountNumber { get; set; }

        [Required(ErrorMessage = "El número de tarjeta de crédito es requerido.")]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "El número de tarjeta debe contener 16 dígitos.")]
        public string CreditCardNumber { get; set; }

        [Required(ErrorMessage = "El monto a pagar es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
        public decimal Amount { get; set; }
    }
}
