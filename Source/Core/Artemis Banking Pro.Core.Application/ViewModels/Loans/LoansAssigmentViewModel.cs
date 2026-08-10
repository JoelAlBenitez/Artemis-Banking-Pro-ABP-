using ArtemisBankingPro.Core.Domain.Common.Enum;
using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Loans
{
    public sealed class LoansAssigmentViewModel
    {

        [Required(ErrorMessage = "Debe seleccionar un cliente para continuar.")]
        public required string CustomerId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un plazo valido.")]
        [Range(6, 60, ErrorMessage = "El plazo debe encontrarse en valor de 6 a 60 meses, en plazos distribuidos en rangos de 6 meses.")]
        [EnumDataType(typeof(TermMonths), ErrorMessage = "Debe selecciona un valor valido para el plazo del prestamo.")]
        public required TermMonths TermLoans { get; set; }

        [Required(ErrorMessage = "Debe indicar una cantidad para el prestamo.")]
        [Range(typeof(decimal), "0.01", "999999999999.99", ErrorMessage = "Debe ingresar un valor mayor a 0.")]
        public required decimal AmmountLoans { get; set; }

        [Required(ErrorMessage = "Debe indicar un interes anual para el prestamo.")]
        [Range(typeof(decimal), "0.0", "999999999999.99", ErrorMessage = "Debe ingresar un valor mayor o igual a 0.")]
        public required decimal AnnualInterestRate { get; set; }


        public bool ConfirmHighRisk { get; set; }
    }
}
