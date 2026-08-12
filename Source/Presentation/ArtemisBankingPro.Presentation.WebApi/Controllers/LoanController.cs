using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using Artemis_Banking_Pro.Core.Application.Features.Loans.Commands.CreateLoan;
using Artemis_Banking_Pro.Core.Application.Features.Loans.Commands.UpdateLoanRate;
using Artemis_Banking_Pro.Core.Application.Features.Loans.Queries.GetAllLoans;
using Artemis_Banking_Pro.Core.Application.Features.Loans.Queries.GetLoanById;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers
{
    [Route("api/loan")]
    [Authorize(Roles = nameof(Roles.Administrador))]
    [SwaggerTag("Gestión administrativa de los préstamos")]
    public class LoanController : BaseApiController
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedApiResponse<LoanListItemDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [SwaggerOperation(
            Summary = "Obtener listado de préstamos",
            Description = "Listado paginado del más reciente al más antiguo. Por defecto muestra los activos")]
        public async Task<IActionResult> Get([FromQuery] GetAllLoansQuery query)
        {
            return Ok(await Mediator.Send(query));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(LoanCreatedDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(HighRiskConflictDto))]
        [SwaggerOperation(
            Summary = "Asignar préstamo a cliente",
            Description = "Crea el préstamo, genera su tabla de amortización y acredita el monto en la cuenta principal del cliente")]
        public async Task<IActionResult> Create([FromBody] CreateLoanCommand command)
        {
            var result = await Mediator.Send(command);

            //El alto riesgo no es un error de infraestructura: devuelve los montos que el
            //administrador necesita para decidir si confirma la asignación.
            if (result.HighRisk is not null)
                return Conflict(result.HighRisk);

            return CreatedAtAction(nameof(GetById), new { id = result.Loan!.Id }, result.Loan);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoanDetailApiDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Obtener detalle de préstamo y tabla de amortización",
            Description = "Detalle del préstamo con el estado de cada una de sus cuotas")]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await Mediator.Send(new GetLoanByIdQuery { Id = id }));
        }

        [HttpPatch("{id:int}/rate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Editar tasa de interés de préstamo",
            Description = "Actualiza la tasa y recalcula únicamente las cuotas futuras pendientes")]
        public async Task<IActionResult> UpdateRate(int id, [FromBody] UpdateLoanRateCommand command)
        {
            command.Id = id;
            await Mediator.Send(command);
            return NoContent();
        }
    }
}
