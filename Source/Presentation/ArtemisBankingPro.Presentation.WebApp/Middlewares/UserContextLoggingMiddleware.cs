// Middleware encargado de empujar al contexto de log el usuario autenticado y su rol
// (campos obligatorios del log).
//
// Queda comentado a propósito: depende de ICurrentUserService, que pertenece al proyecto
// Identity y todavía no existe en la solución. Se habilitará junto con la autenticación,
// registrándolo en el pipeline DESPUÉS de UseAuthentication y antes de los endpoints.

//using Serilog.Context;

//namespace ArtemisBankingPro.Presentation.WebApp.Middlewares
//{
//    public sealed class UserContextLoggingMiddleware
//    {
//        public const string UserLogPropertyName = "UserName";
//        public const string RoleLogPropertyName = "UserRole";

//        private readonly RequestDelegate _next;

//        public UserContextLoggingMiddleware(RequestDelegate next)
//        {
//            _next = next;
//        }

//        public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUserService)
//        {
//            using (LogContext.PushProperty(UserLogPropertyName, currentUserService.UserName))
//            using (LogContext.PushProperty(RoleLogPropertyName, currentUserService.Role))
//            {
//                await _next(context);
//            }
//        }
//    }

//    public static class UserContextLoggingMiddlewareExtensions
//    {
//        public static IApplicationBuilder UseUserContextLogging(this IApplicationBuilder app)
//            => app.UseMiddleware<UserContextLoggingMiddleware>();
//    }
//}
