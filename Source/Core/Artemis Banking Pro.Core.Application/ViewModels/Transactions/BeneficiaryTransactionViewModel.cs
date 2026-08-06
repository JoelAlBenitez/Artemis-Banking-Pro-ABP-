using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Transactions
{
    public class BeneficiaryTransactionViewModel
    {
        [SetsRequiredMembers]
        public BeneficiaryTransactionViewModel()
        {
            SourceAccountNumber = null!;
        }

        [Required(ErrorMessage = "Debe seleccionar la cuenta de ahorro de origen.")]
        public required string SourceAccountNumber { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el beneficiario destino.")]
        public int BeneficiaryId { get; set; }

        [Required(ErrorMessage = "El monto a transferir es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
        public decimal Amount { get; set; }
    }
}
