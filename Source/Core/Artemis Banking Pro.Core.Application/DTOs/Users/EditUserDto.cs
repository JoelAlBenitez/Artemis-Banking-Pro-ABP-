namespace ArtemisBankingPro.Core.Application.DTOs.Users
{
    public class EditUserDto
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public required string IDCARD { get; set; }
        public required string Email { get; set; }
        public required string UserName { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmNewPassword { get; set; }
        public decimal? AdditionalAmount { get; set; }
    }
}
