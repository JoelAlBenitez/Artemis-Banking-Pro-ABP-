using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Core.Application.DTOs.Account
{
    public class ResetPasswordRequest
    {
        [Required(AllowEmptyStrings = false)]
        public required string Email { get; set; }

        [Required(AllowEmptyStrings = false)]
        public required string Token { get; set; }

        [Required(AllowEmptyStrings = false)]
        public required string Password { get; set; }

        [Required(AllowEmptyStrings = false)]
        public required string ConfirmPassword { get; set; }
    }
}
