namespace ArtemisBankingPro.Core.Application.DTOs.Account
{
    public class LoginApiDtoResponse
    {
        public required string Token { get; set; }
        public required string UserName { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime Expiration { get; set; }
        public bool HasError { get; set; }
        public string? Error { get; set; }
        public bool Forbidden { get; set; }
    }
}
