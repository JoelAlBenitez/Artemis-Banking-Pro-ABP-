using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Loans
{
    public sealed class EditAnnualInterestRateViewModel
    {
    
        [Required(ErrorMessage = "Debe indicar un prestamo valido.")]
        public required int LoansId { get; set; }

        [Required(ErrorMessage = "Debe indicar un interes anual para el prestamo.")]
        [Range(0.0, 999999999999.99, ErrorMessage = "Debe ingresar un valor mayor o igual a 0.")]
        public required decimal AnnualInterestRate { get; set; }
    }
}
