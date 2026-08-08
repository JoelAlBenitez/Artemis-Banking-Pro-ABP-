namespace ArtemisBankingPro.Core.Application.ViewModels.Account
{
    //Pantalla de Acceso denegado: el mensaje es fijo y el enlace de regreso depende del rol
    //del usuario autenticado (Login si no hay sesión).
    public sealed class AccessDeniedViewModel
    {
        public string Message { get; set; } = "No posee permisos para acceder a esta sección.";
        public required string HomeController { get; set; }
        public required string HomeAction { get; set; }
    }
}
