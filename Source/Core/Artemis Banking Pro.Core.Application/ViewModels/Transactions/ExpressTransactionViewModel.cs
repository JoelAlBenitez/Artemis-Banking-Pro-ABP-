using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Transactions
{
    public class ExpressTransactionViewModel
    {
        [SetsRequiredMembers]
        public ExpressTransactionViewModel()
        {
            SourceAccountNumber = null!;
            DestinationAccountNumber = null!;
        }

        [Required(ErrorMessage = "Debe seleccionar la cuenta de ahorro de origen.")]
        public required string SourceAccountNumber { get; set; }

        [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
        [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta destino debe contener exactamente 9 dígitos.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "El número de cuenta destino solo debe contener números.")]
        public required string DestinationAccountNumber { get; set; }

        [Required(ErrorMessage = "El monto a transferir es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
        public decimal Amount { get; set; }
    }
}
