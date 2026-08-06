namespace ArtemisBankingPro.Core.Application.DTOs.Users
{
    public class UserDetailDto
    {
        public required string Id { get; set; }
        public required string UserName { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public required string IDCARD { get; set; }
        public required string Email { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmNewPassword { get; set; }
    }
}
