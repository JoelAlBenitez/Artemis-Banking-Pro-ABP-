using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class LoanPaymentViewModel
    {
        [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
        public string SourceAccountNumber { get; set; }

        [Required(ErrorMessage = "El número del préstamo es requerido.")]
        [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de préstamo debe contener 9 dígitos.")]
        public string LoanNumber { get; set; }

        [Required(ErrorMessage = "El monto a pagar es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
        public decimal Amount { get; set; }
    }
}
