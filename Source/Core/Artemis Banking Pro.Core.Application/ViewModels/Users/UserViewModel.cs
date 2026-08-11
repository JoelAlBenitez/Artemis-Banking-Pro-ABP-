using Artemis_Banking_Pro.Core.Application.ViewModels.Base;

namespace Artemis_Banking_Pro.Core.Application.ViewModels.Users
{
    //Fila del listado del mantenimiento de usuarios.
    public sealed class UserViewModel : BaseViewModel<string>
    {
        public required string FullName { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string IDCARD { get; set; }
        //Etiqueta de presentación: Activo / Inactivo
        public required string State { get; set; }
        public required string TypeUser { get; set; }
        //La acción del listado alterna entre Activar e Inactivar según este indicador
        public bool IsActive { get; set; }
    }
}
