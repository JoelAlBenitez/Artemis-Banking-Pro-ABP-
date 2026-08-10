using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Core.Application.DTOs.Account
{
    public class RegisterRequest
    {
        [Required(AllowEmptyStrings = false)]
        public required string FirstName { get; set; }

        [Required(AllowEmptyStrings = false)]
        public required string LastName { get; set; }

        [Required(AllowEmptyStrings = false)]
        public required string IDCARD { get; set; }

        [Required(AllowEmptyStrings = false)]
        public required string Email { get; set; }

        [Required(AllowEmptyStrings = false)]
        public required string UserName { get; set; }

        [Required(AllowEmptyStrings = false)]
        public required string Password { get; set; }

        [Required(AllowEmptyStrings = false)]
        public required string ConfirmPassword { get; set; }

        [Required(AllowEmptyStrings = false)]
        public required string Role { get; set; }

        public decimal? InitialAmount { get; set; }
        public string? Origin { get; set; }
    }
}
