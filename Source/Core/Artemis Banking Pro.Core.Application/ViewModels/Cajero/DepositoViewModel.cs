using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Core.Application.ViewModels.Cajero
{
    public class DepositoViewModel
    {
        [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
        public string NumeroCuentaDestino { get; set; }

        [Required(ErrorMessage = "El monto a depositar es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a depositar debe ser mayor que cero.")]
        public decimal Monto { get; set; }
    }
}
