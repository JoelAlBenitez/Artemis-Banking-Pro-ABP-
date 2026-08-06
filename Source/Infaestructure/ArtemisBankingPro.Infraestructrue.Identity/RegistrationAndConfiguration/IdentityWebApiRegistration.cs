using ArtemisBankingPro.Infraestructrue.Identity.Context;
using ArtemisBankingPro.Infraestructrue.Identity.Entities;
using ArtemisBankingPro.Infraestructrue.Identity.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.Contracts.Users.Password;
using ArtemisBankingPro.Core.Application.Contracts.Users.ExternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.InternalUsers;
using ArtemisBankingPro.Core.Application.Contracts.Users.Tokens;
using ArtemisBankingPro.Infraestructrue.Identity.Services.Auth;
using ArtemisBankingPro.Infraestructrue.Identity.Services.Tokens;

namespace ArtemisBankingPro.Infraestructrue.Identity.RegistrationAndConfiguration
{
    public static class IdentityWebApiRegistration
    {
        public static void AddWebApiIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            GeneralConfiguration.AddGeneralConfiguration(services, configuration);
            services.AddScoped<IAuthWebApiService, AuthWebApiService>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            // Session
            services.AddHttpContextAccessor();
            services.AddScoped<ArtemisBankingPro.Core.Application.Contracts.Users.Session.ICurrentUserService, ArtemisBankingPro.Infraestructrue.Identity.Services.Session.CurrentUserService>();

            services.Configure<ArtemisBankingPro.Core.Domain.Settings.JwtSettings>(configuration.GetSection("JwtSettings"));

            #region Identity Options

            services.Configure<IdentityOptions>(opt =>
            {
                opt.User.RequireUniqueEmail = true;

                // Contraseñas simples permitidas (la biblia no exige complejidad)
                opt.Password.RequireDigit = false;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireLowercase = false;
                opt.Password.RequireUppercase = false;
                opt.Password.RequiredLength = 1;
            });

            #endregion

            #region Token Providers

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

            #region JWT Authentication

            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opt =>
            {
                opt.RequireHttpsMetadata = false;
                opt.SaveToken = false;

                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidAudience = configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"] ?? ""))
                };

                opt.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        context.NoResult();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var result = JsonSerializer.Serialize(new
                        {
                            HasError = true,
                            Error = "No tiene autorización para acceder a este recurso."
                        });
                        return context.Response.WriteAsync(result);
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var result = JsonSerializer.Serialize(new
                        {
                            HasError = true,
                            Error = "No tiene autorización para acceder a este recurso."
                        });
                        return context.Response.WriteAsync(result);
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        var result = JsonSerializer.Serialize(new
                        {
                            HasError = true,
                            Error = "Acceso denegado. No tiene permisos para utilizar este recurso."
                        });
                        return context.Response.WriteAsync(result);
                    }
                };
            })
            .AddCookie(IdentityConstants.ApplicationScheme, opt =>
            {
                opt.ExpireTimeSpan = TimeSpan.FromMinutes(180);
            });

            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();
            });

            services.Configure<SecurityStampValidatorOptions>(opt =>
            {
                opt.ValidationInterval = TimeSpan.FromMinutes(5);
            });

            services.AddScoped<ISecurityStampValidator, SecurityStampValidator<ApplicationUser>>();

            #endregion
        }
    }
}

