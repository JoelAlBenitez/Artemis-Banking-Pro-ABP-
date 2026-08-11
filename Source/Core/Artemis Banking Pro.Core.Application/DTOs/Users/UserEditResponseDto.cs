namespace ArtemisBankingPro.Core.Application.DTOs.Users
{
    //Respuesta de la carga de la pantalla de edición: el detalle del usuario solo viaja
    //cuando las reglas del mantenimiento permiten editarlo (existe, no es Comercio y no es
    //la propia cuenta del administrador autenticado).
    public class UserEditResponseDto : UserOperationResponseDto
    {
        public UserDetailDto? User { get; set; }
    }
}
