using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArtemisBankingPro.Infraestructrue.Identity.RegistrationAndConfiguration
{
    public class ResetPasswordTokenProviderOptions : DataProtectionTokenProviderOptions { }

    public class ResetPasswordTokenProvider<TUser> : DataProtectorTokenProvider<TUser> where TUser : class
    {
        public ResetPasswordTokenProvider(
            IDataProtectionProvider dataProtectionProvider, 
            IOptions<ResetPasswordTokenProviderOptions> options, 
            ILogger<DataProtectorTokenProvider<TUser>> logger)
            : base(dataProtectionProvider, options, logger) { }
    }
}
