using Serilog.Context;

namespace ArtemisBankingPro.Presentation.WebApi.Middlewares
{
    public sealed class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";
        public const string LogPropertyName = "CorrelationId";

        private const int MaxLength = 64;

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = ResolveCorrelationId(context);
            context.Items[LogPropertyName] = correlationId;

            using (LogContext.PushProperty(LogPropertyName, correlationId))
            {
                await _next(context);
            }
        }

        private static string ResolveCorrelationId(HttpContext context)
        {
            var incoming = context.Request.Headers[HeaderName].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(incoming) && incoming.Length <= MaxLength
                && incoming.All(c => char.IsLetterOrDigit(c) || c == '-'))
            {
                return incoming;
            }

            return Guid.NewGuid().ToString("N");
        }
    }

    public static class CorrelationIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
            => app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
