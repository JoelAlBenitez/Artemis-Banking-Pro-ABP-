using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
using Serilog.Context;
using System.Security.Claims;

namespace ArtemisBankingPro.Presentation.WebApi.Middlewares
{
    //Empuja al contexto de log el usuario autenticado y su rol: campos obligatorios 3 y 4 del
    //documento funcional. Se registra DESPUÉS de UseAuthentication, o el usuario todavía no
    //está resuelto y todo saldría como anónimo.
    public sealed class UserContextLoggingMiddleware
    {
        public const string UserLogPropertyName = "UserName";
        public const string RoleLogPropertyName = "UserRole";

        //Las peticiones sin autenticar también se registran: la ausencia de usuario es un dato,
        //no un hueco en el log.
        public const string AnonymousUser = "Anonimo";
        public const string WithoutRole = "SinRol";

        private readonly RequestDelegate _next;

        public UserContextLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        //ICurrentUserService es scoped: se inyecta por invocación, no por constructor.
        public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUserService)
        {
            var userName = ResolveUserName(currentUserService);
            var userRole = ResolveRoles(currentUserService);

            using (LogContext.PushProperty(UserLogPropertyName, userName))
            using (LogContext.PushProperty(RoleLogPropertyName, userRole))
            {
                await _next(context);
            }
        }

        private static string ResolveUserName(ICurrentUserService currentUserService)
        {
            var userName = currentUserService.UserName;

            //Un usuario autenticado sin claim de nombre igual debe quedar identificado por su Id
            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = currentUserService.UserId;
            }

            return string.IsNullOrWhiteSpace(userName) ? AnonymousUser : userName;
        }

        //La interfaz expone una lista: un usuario puede tener más de un rol.
        private static string ResolveRoles(ICurrentUserService currentUserService)
        {
            var roles = currentUserService.Roles;

            if (roles is null || roles.Count == 0)
            {
                return WithoutRole;
            }

            return string.Join(", ", roles);
        }

        //El log de la petición se escribe fuera del alcance de ICurrentUserService, que es scoped:
        //allí el usuario se lee del principal, ya resuelto por la autenticación.
        public static string ResolveUserName(ClaimsPrincipal? principal)
        {
            var userName = principal?.FindFirstValue(ClaimTypes.Name)
                           ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            return string.IsNullOrWhiteSpace(userName) ? AnonymousUser : userName;
        }

        public static string ResolveRoles(ClaimsPrincipal? principal)
        {
            var roles = principal?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList();

            return roles is null || roles.Count == 0 ? WithoutRole : string.Join(", ", roles);
        }
    }

    public static class UserContextLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserContextLogging(this IApplicationBuilder app)
            => app.UseMiddleware<UserContextLoggingMiddleware>();
    }
}
