using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using ArtemisBankingPro.Infraestructrue.Identity.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Web;

namespace ArtemisBankingPro.Infraestructrue.Identity.Services.Tokens
{
    public class GenerateTokens : IGenerateTokens
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public GenerateTokens(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<string> GenerateTokenConfirmEmailAsync(ApplicationUser user, string origin)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(token);
            var verificationUri = $"{origin}/Account/ConfirmAccountEmail?userId={user.Id}&token={encodedToken}";
            return verificationUri;
        }

        public async Task<string> GenerateTokenResetPasswordAsync(ApplicationUser user, string origin)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return token;
        }
    }
}
