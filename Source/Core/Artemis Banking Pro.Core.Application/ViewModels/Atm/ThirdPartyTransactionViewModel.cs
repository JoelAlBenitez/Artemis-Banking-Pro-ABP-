using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Core.Application.ViewModels.Atm
{
    public class ThirdPartyTransactionViewModel
    {
        [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
        public string SourceAccountNumber { get; set; }

        [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
        public string DestinationAccountNumber { get; set; }

        [Required(ErrorMessage = "El monto de la transacción es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto de la transacción debe ser mayor que cero.")]
        public decimal Amount { get; set; }
    }
}
