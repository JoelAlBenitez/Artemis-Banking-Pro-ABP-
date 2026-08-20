using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.SavingsAccounts
{
    public sealed class SavingsAccountAssignmentViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un cliente para continuar.")]
        public required string CustomerId { get; set; }

        [Required(ErrorMessage = "El balance inicial es requerido.")]
        [Range(0, 999999999999.99, ErrorMessage = "El balance inicial no puede ser negativo.")]
        public required decimal InitialBalance { get; set; }

        //Solo lectura: el nombre del cliente elegido en el paso 1 se muestra en el formulario
        public string? FullNameCustomer { get; set; }
    }
}
