namespace ArtemisBankingPro.Core.Application.DTOs.Account
{
    public class RegisterResponse
    {
        public string? UserId { get; set; }
        public bool HasError { get; set; }
        public string? Error { get; set; }

        //Cédula, correo o nombre de usuario ya registrados: la Web API lo traduce a 409 Conflict
        public bool Conflict { get; set; }
    }
}
