using ArtemisBankingPro.Infraestructrue.Identity.Context;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using ArtemisBankingPro.Infraestructrue.Identity.Errors;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.Contracts.Users.Password;
using ArtemisBankingPro.Core.Application.Contracts.Users.ExternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.InternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.Tokens;
using ArtemisBankingPro.Infraestructrue.Identity.Services.Auth;

namespace ArtemisBankingPro.Infraestructrue.Identity.RegistrationAndConfiguration
{
    public static class IdentityWebAppRegistration
    {
        public static void AddWebAppIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            GeneralConfiguration.AddGeneralConfiguration(services, configuration);
            services.AddScoped<IAuthWebAppService, AuthWebAppService>();

            // Session
            services.AddHttpContextAccessor();
            services.AddScoped<ArtemisBankingPro.Core.Application.Contracts.Users.Session.ICurrentUserService, ArtemisBankingPro.Infraestructrue.Identity.Services.Session.CurrentUserService>();

            #region Identity Options

            services.Configure<IdentityOptions>(opt =>
            {
                opt.User.RequireUniqueEmail = true;
                opt.SignIn.RequireConfirmedEmail = true;

                // Contraseñas simples permitidas (la biblia no exige complejidad)
                opt.Password.RequireDigit = false;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireLowercase = false;
                opt.Password.RequireUppercase = false;
                opt.Password.RequiredLength = 1;
            });

            #endregion

            #region Cookie Authentication

            services.AddAuthentication(opt =>
            {
                opt.DefaultScheme = IdentityConstants.ApplicationScheme;
                opt.DefaultSignOutScheme = IdentityConstants.ApplicationScheme;
                opt.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
            })
            .AddCookie(IdentityConstants.ApplicationScheme, opt =>
            {
                opt.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                opt.SlidingExpiration = true;
                opt.Cookie.HttpOnly = true;
                opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                opt.Cookie.SameSite = SameSiteMode.Lax;
                opt.AccessDeniedPath = "/Account/AccessDenied";
                opt.LoginPath = "/Account/Login";
                opt.Events.OnRedirectToLogin = context =>
                {
                    bool hadSession = context.Request.Cookies.ContainsKey(context.Options.Cookie.Name!);
                    if (hadSession)
                        context.Response.Redirect("/Account/Login?expired=true");
                    else
                        context.Response.Redirect("/Account/Login?denied=true");
                    return Task.CompletedTask;
                };
                opt.Events.OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync;
            })
            .AddCookie(IdentityConstants.TwoFactorRememberMeScheme)
            .AddCookie(IdentityConstants.TwoFactorUserIdScheme)
            .AddCookie(IdentityConstants.ExternalScheme);

            #endregion

            #region Security Stamp & Token Providers

            services.Configure<SecurityStampValidatorOptions>(opt =>
            {
                opt.ValidationInterval = TimeSpan.FromMinutes(5);
            });

            services.AddScoped<ISecurityStampValidator, SecurityStampValidator<ApplicationUser>>();

            // Token para Confirmación de Cuenta (Caducidad extendida de 7 días, un solo uso)
            services.Configure<DataProtectionTokenProviderOptions>(opt =>
            {
                opt.TokenLifespan = TimeSpan.FromDays(7);
            });

            // Token específico para Restablecer Contraseña (30 minutos exactos según documento)
            services.Configure<ResetPasswordTokenProviderOptions>(opt =>
            {
                opt.TokenLifespan = TimeSpan.FromMinutes(30);
            });

            #endregion

            #region Identity Core

            services.AddIdentityCore<ApplicationUser>(opt =>
            {
                opt.Tokens.PasswordResetTokenProvider = "ResetPassword";
                opt.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultProvider;
            })
            .AddRoles<IdentityRole>()
            .AddSignInManager()
            .AddErrorDescriber<SpanishIdentityErrorDescriber>()
            .AddEntityFrameworkStores<IdentityContext>()
            .AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>(TokenOptions.DefaultProvider)
            .AddTokenProvider<ResetPasswordTokenProvider<ApplicationUser>>("ResetPassword");

            #endregion
            // Punto 5: FallbackPolicy — toda ruta sin [AllowAnonymous] exige autenticación
            services.AddAuthorization(opt =>
            {
                opt.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
        }
    }
}

