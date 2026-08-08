namespace ArtemisBankingPro.Core.Application.DTOs.Users
{
    //Resultado de las operaciones de mantenimiento de usuarios que solo necesitan informar
    //éxito o el mensaje exacto exigido por el documento funcional.
    //Los indicadores permiten a la Web API responder 404 o 409 sin inspeccionar el texto.
    public class UserOperationResponseDto
    {
        public bool HasError { get; set; }
        public string? Error { get; set; }
        public bool NotFound { get; set; }
        public bool Conflict { get; set; }
    }
}
