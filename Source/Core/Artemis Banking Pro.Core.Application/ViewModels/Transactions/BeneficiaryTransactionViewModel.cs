using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Transactions
{
    public class BeneficiaryTransactionViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar la cuenta de ahorro de origen.")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar el beneficiario destino.")]
        public int BeneficiaryId { get; set; }

        [Required(ErrorMessage = "El monto a transferir es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
        public decimal Amount { get; set; }
    }
}
