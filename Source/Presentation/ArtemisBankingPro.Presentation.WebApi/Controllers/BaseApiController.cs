using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers
{
    /// <summary>
    /// Controlador base de la Web API. Expone el mediador que resuelve los Commands y Queries.
    /// </summary>
    /// <remarks>
    /// La ruta no se declara aquí: el documento funcional define rutas heterogéneas
    /// (/account, /api/users, /pay), por lo que cada controlador fija la suya.
    /// </remarks>
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        private IMediator? _mediator;

        /// <summary>
        /// Mediador resuelto desde el ámbito de la solicitud actual.
        /// </summary>
        protected IMediator Mediator =>
            _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();
    }
}
