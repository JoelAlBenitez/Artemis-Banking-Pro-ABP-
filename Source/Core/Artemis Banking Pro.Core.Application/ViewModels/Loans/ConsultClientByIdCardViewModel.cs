using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Loans
{
    public sealed class ConsultClientByIdCardViewModel
    {
        [Required(ErrorMessage = "Debe ingresar una cédula valida. (Sin guiones y Dominicana).")]
        [StringLength(11, ErrorMessage = "Debe ingresar una cédula no mayor a 11 digitos sin guiones.")]
        public required string IdCard {  get; set; }
    }
}
