using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Users
{
    //Formulario de creación de usuarios del mantenimiento administrativo.
    public sealed class SaveUserViewModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "El nombre es requerido.")]
        public required string FirstName { get; set; }

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

        [Required(AllowEmptyStrings = false, ErrorMessage = "La contraseña es requerida.")]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Debe confirmar la contraseña.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "La contraseña y la confirmación de contraseña deben coincidir.")]
        public required string ConfirmPassword { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Debe seleccionar el tipo de usuario.")]
        public required string Role { get; set; }

        //Solo aplica al rol Cliente: es el saldo de apertura de su cuenta de ahorro principal
        [Range(typeof(decimal), "0", "999999999999.99", ErrorMessage = "El monto inicial no puede ser negativo.")]
        public decimal? InitialAmount { get; set; }

        //Roles del combo. No viaja en el formulario: lo repuebla el controlador en cada carga.
        public IReadOnlyCollection<string> AvailableRoles { get; set; } = Array.Empty<string>();
    }
}
