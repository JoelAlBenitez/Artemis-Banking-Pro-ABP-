namespace ArtemisBankingPro.Core.Application.DTOs.Account
{
    /// <summary>
    /// Token JWT devuelto por el inicio de sesión de la Web API.
    /// </summary>
    public class JwtTokenDto
    {
        /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
        public required string Jwt { get; set; }
    }
}
