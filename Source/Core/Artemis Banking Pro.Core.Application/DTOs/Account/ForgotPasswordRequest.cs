using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Core.Application.DTOs.Account
{
    public class ForgotPasswordRequest
    {
        [Required(AllowEmptyStrings = false)]
        public required string UserName { get; set; }
    }
}
