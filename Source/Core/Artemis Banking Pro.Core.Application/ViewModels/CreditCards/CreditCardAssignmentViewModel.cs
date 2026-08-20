using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.CreditCards
{
    public sealed class CreditCardAssignmentViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un cliente para continuar.")]
        public required string CustomerId { get; set; }

        [Required(ErrorMessage = "El límite de crédito es requerido.")]
        [Range(0.01, 999999999999.99, ErrorMessage = "El límite de crédito debe ser mayor que cero.")]
        public required decimal CreditLimit { get; set; }
    }
}
