using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Features.SavingsAccounts.Commands.CancelSavingsAccount;
using Artemis_Banking_Pro.Core.Application.Features.SavingsAccounts.Commands.CreateSecondaryAccount;
using Artemis_Banking_Pro.Core.Application.Features.SavingsAccounts.Queries.GetAccountTransactions;
using Artemis_Banking_Pro.Core.Application.Features.SavingsAccounts.Queries.GetAllSavingsAccounts;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers
{
    [Route("api/savings-account")]
    [Authorize(Roles = nameof(Roles.Administrador))]
    [SwaggerTag("Gestión administrativa de las cuentas de ahorro")]
    public class SavingsAccountController : BaseApiController
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedApiResponse<SavingsAccountListItemDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [SwaggerOperation(
            Summary = "Obtener listado de cuentas de ahorro",
            Description = "Listado paginado de cuentas, de la más reciente a la más antigua. Por defecto muestra las activas")]
        public async Task<IActionResult> Get([FromQuery] GetAllSavingsAccountsQuery query)
        {
            return Ok(await Mediator.Send(query));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(SavingsAccountCreatedDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [SwaggerOperation(
            Summary = "Asignar cuenta de ahorro secundaria a cliente",
            Description = "Crea una cuenta secundaria activa para un cliente con cuenta principal activa. Un balance inicial mayor que cero se registra como CRÉDITO")]
        public async Task<IActionResult> Create([FromBody] CreateSecondaryAccountCommand command)
        {
            var account = await Mediator.Send(command);
            return CreatedAtAction(nameof(GetTransactions),
                new { accountNumber = account.AccountNumber }, account);
        }

        [HttpGet("{accountNumber}/transactions")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountTransactionsDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Obtener detalles de transacciones por cuenta",
            Description = "Historial paginado de la cuenta, de la transacción más reciente a la más antigua")]
        public async Task<IActionResult> GetTransactions(
            string accountNumber, [FromQuery] GetAccountTransactionsQuery query)
        {
            query.AccountNumber = accountNumber;
            return Ok(await Mediator.Send(query));
        }

        [HttpPatch("{accountNumber}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Cancelar cuenta de ahorro secundaria",
            Description = "Solo cancela cuentas secundarias activas. El balance disponible se transfiere a la cuenta principal del cliente")]
        public async Task<IActionResult> Cancel(string accountNumber)
        {
            await Mediator.Send(new CancelSavingsAccountCommand { AccountNumber = accountNumber });
            return NoContent();
        }
    }
}
