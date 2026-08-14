namespace ArtemisBankingPro.Core.Application.DTOs.Account
{
    public class ResetPasswordApiRequest
    {
        public required string UserId { get; set; }
        public required string Token { get; set; }
        public required string Password { get; set; }
        public required string ConfirmPassword { get; set; }
    }
}
