namespace ArtemisBankingPro.Core.Application.DTOs.Users
{
    //Resultado de las operaciones de mantenimiento de usuarios que solo necesitan informar
    //éxito o el mensaje exacto exigido por el documento funcional.
    public class UserOperationResponseDto
    {
        public bool HasError { get; set; }
        public string? Error { get; set; }
        public bool NotFound { get; set; }
    }
}
