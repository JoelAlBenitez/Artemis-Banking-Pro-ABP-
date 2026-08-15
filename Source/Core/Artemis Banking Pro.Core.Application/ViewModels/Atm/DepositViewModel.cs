using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class DepositViewModel
    {
        [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
        [Display(Name = "Número de cuenta destino")]
        public string DestinationAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto a depositar es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a depositar debe ser mayor que cero.")]
        [Display(Name = "Monto a depositar")]
        public decimal Amount { get; set; }
    }
}
