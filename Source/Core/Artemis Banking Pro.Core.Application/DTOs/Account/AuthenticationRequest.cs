using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Core.Application.DTOs.Account
{
    public class AuthenticationRequest
    {
        [Required(AllowEmptyStrings = false)]
        public required string UserName { get; set; }

        [Required(AllowEmptyStrings = false)]
        public required string Password { get; set; }
    }
}
