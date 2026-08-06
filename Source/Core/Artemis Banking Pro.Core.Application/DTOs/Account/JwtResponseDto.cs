namespace ArtemisBankingPro.Core.Application.DTOs.Account
{
    public class JwtResponseDto
    {
        public required string Token { get; set; }
        public DateTime Expiration { get; set; }
    }
}
