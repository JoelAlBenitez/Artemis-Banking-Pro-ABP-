using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Loans
{
    public sealed class LoansAssigmentViewModel
    {

        [Required(ErrorMessage = "Debe seleccionar un cliente valido.")]
        public required int CustomerId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un plazo valido.")]
        [Range(6, 60, ErrorMessage = "El plazo debe encontrarse en valor de 6 a 60 meses, en plazos distribuidos en rangos de 6 meses.")]
        public required int TermLoans { get; set; }

        [Required(ErrorMessage = "Debe indicar una cantidad para el prestamo.")]
        [Range(typeof(decimal), "0.01", "999999999999.99", ErrorMessage = "Debe ingresar un valor mayor a 0.")]
        public required decimal AmmountLoans { get; set; }

        [Required(ErrorMessage = "Debe indicar un interes anual para el prestamo.")]
        [Range(typeof(decimal), "0.0", "999999999999.99", ErrorMessage = "Debe ingresar un valor mayor o igual a 0.")]
        public required decimal AnnualInterestRate { get; set; }
    }
}
