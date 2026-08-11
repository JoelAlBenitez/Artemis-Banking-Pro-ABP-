using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace ArtemisBankingPro.Presentation.WebApi.Middlewares
{
    
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(IProblemDetailsService problemDetailsService,
                                      ILogger<GlobalExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
                                                    CancellationToken cancellationToken)
        {
            var statusCode = GetStatusCode(exception);
            var action = $"{httpContext.Request.Method} {httpContext.Request.Path}";

            LogException(httpContext, exception, action, statusCode);

            httpContext.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = GetTitle(statusCode),
                Detail = GetDetail(exception, statusCode),
                Instance = action
            };

            if (exception is ApplicationLayerException applicationException && applicationException.Errors.Count > 0)
            {
                problemDetails.Extensions["errors"] = applicationException.Errors
                    .Select(error => new { code = error.Code, description = error.Description })
                    .ToList();
            }

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = problemDetails
            });
        }

        public static int GetStatusCode(Exception exception) => exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            //Una regla de negocio incumplida es una solicitud inválida: el documento reserva
            //el 409 para los choques de estado, que viajan en ConflictException.
            BusinessRuleException => StatusCodes.Status400BadRequest,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            ForbiddenException => StatusCodes.Status403Forbidden,
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

      
        private void LogException(HttpContext httpContext, Exception exception, string action, int statusCode)
        {
            var correlationId = httpContext.Items[CorrelationIdMiddleware.LogPropertyName];

            using (LogContext.PushProperty(CorrelationIdMiddleware.LogPropertyName, correlationId))
            {
                if (statusCode >= StatusCodes.Status500InternalServerError)
                {
                    _logger.LogError(exception, LogEvents.Templates.UnhandledError, action, LogEvents.Results.Failed);
                    return;
                }

                _logger.LogWarning(exception, "Solicitud {Accion} finalizada con estado {StatusCode}. Resultado: {Resultado}",
                    action, statusCode, LogEvents.Results.Rejected);
            }
        }

        private static string GetTitle(int statusCode) => statusCode switch
        {
            StatusCodes.Status400BadRequest => "Solicitud inválida",
            StatusCodes.Status401Unauthorized => "No autenticado",
            StatusCodes.Status403Forbidden => "Acceso denegado",
            StatusCodes.Status404NotFound => "Recurso no encontrado",
            StatusCodes.Status409Conflict => "Conflicto de negocio",
            _ => "Error interno del servidor"
        };

        private static string GetDetail(Exception exception, int statusCode)
            => statusCode >= StatusCodes.Status500InternalServerError
                ? GeneralError.UnexpectedError.Description
                : exception.Message;
    }
}
