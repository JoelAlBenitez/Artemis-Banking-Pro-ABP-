using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class WithdrawalViewModel
    {
        [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
        [Display(Name = "Número de cuenta origen")]
        public string OriginAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto a retirar es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a retirar debe ser mayor que cero.")]
        [Display(Name = "Monto a retirar")]
        public decimal Amount { get; set; }
    }
}
