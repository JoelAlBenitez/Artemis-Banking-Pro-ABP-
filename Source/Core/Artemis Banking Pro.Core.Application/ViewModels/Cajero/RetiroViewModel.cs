using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Core.Application.ViewModels.Cajero
{
    public class RetiroViewModel
    {
        [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
        public string NumeroCuentaOrigen { get; set; }

        [Required(ErrorMessage = "El monto a retirar es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a retirar debe ser mayor que cero.")]
        public decimal Monto { get; set; }
    }
}
