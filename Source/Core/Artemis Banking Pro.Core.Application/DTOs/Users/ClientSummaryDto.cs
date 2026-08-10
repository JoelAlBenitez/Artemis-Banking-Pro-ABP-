namespace ArtemisBankingPro.Core.Application.DTOs.Users
{
    public class ClientSummaryDto
    {
        //Identificador del cliente en Identity: es la clave con la que los demás módulos
        //relacionan sus productos (cuentas, préstamos, tarjetas), que no tienen FK física.
        public required string Id { get; set; }
        public required string IDCARD { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
    }
}
