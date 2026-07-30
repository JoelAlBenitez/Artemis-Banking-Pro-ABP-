using Artemis_Banking_Pro.Core.Application.Common;
using Artemis_Banking_Pro.Core.Application.Exceptions;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

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

            LogException(exception, action, statusCode);

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
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            ForbiddenException => StatusCodes.Status403Forbidden,
            NotFoundException => StatusCodes.Status404NotFound,
            BusinessRuleException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        private void LogException(Exception exception, string action, int statusCode)
        {
            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, LogEvents.Templates.UnhandledError, action, LogEvents.Results.Failed);
                return;
            }

            _logger.LogWarning(exception, "Solicitud {Accion} finalizada con estado {StatusCode}. Resultado: {Resultado}",
                action, statusCode, LogEvents.Results.Rejected);
        }

        private static string GetTitle(int statusCode) => statusCode switch
        {
            StatusCodes.Status400BadRequest => "Solicitud inválida",
            StatusCodes.Status401Unauthorized => "No autenticado",
            StatusCodes.Status403Forbidden => "Sin permisos",
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
