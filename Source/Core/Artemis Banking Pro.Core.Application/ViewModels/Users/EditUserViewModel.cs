using Artemis_Banking_Pro.Core.Application.ViewModels.Base;
using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Users
{
    //Formulario de edición de usuarios del mantenimiento administrativo.
    public sealed class EditUserViewModel : BaseViewModel<string>
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "El nombre es requerido.")]
        public required string Name { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "El apellido es requerido.")]
        public required string LastName { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "La cédula es requerida.")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "La cédula debe contener exactamente 11 dígitos sin guiones.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "La cédula solo debe contener números.")]
        public required string IDCARD { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "El correo electrónico es requerido.")]
        [EmailAddress(ErrorMessage = "Debe indicar un correo electrónico valido.")]
        public required string Email { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "El nombre de usuario es requerido.")]
        public required string UserName { get; set; }

        //Si se deja vacía, la contraseña actual no se modifica
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "La contraseña y la confirmación de contraseña deben coincidir.")]
        public string? ConfirmNewPassword { get; set; }

        //Solo aplica al rol Cliente: se acredita a su cuenta de ahorro principal
        [Range(0, 999999999999.99, ErrorMessage = "El monto adicional no puede ser negativo.")]
        public decimal? AdditionalAmount { get; set; }

        //Decide si la pantalla muestra el campo de monto adicional. No viaja en el formulario:
        //lo repuebla el controlador en cada carga.
        public bool IsClient { get; set; }
    }
}
