using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infraestructrue.Identity.Interfaces
{
    public interface IGenerateTokens
    {
        Task<string> GenerateTokenResetPasswordAsync(ApplicationUser user, string origin);
        Task<string> GenerateTokenConfirmEmailAsync(ApplicationUser user, string origin);
    }
}
