# Avance de efectivo — DTOs, servicio y correo

## `CashAdvanceRequestDto` (escritura)

| Campo | Tipo | Nota |
|---|---|---|
| `CreditCardId` | int | Tarjeta origen |
| `SavingsAccountId` | int | Cuenta destino |
| `Amount` | decimal | Monto solicitado (sin interés); > 0 |

## `CashAdvanceDto` (lectura — resultado)

| Campo | Tipo | Nota |
|---|---|---|
| `RequestedAmount` | decimal | Monto acreditado |
| `InterestAmount` | decimal | Interés aplicado |
| `TotalCharged` | decimal | Monto + interés |
| `CardLastFourDigits` | string(4) | Identificación segura de la tarjeta |
| `AccountLastFourDigits` | string(4) o similar | Identificación de la cuenta destino |
| `CreatedAt` | DateTime | Fecha y hora exacta |

## `CashAdvanceViewModel`

```csharp
public class CashAdvanceViewModel : BaseViewModel
{
    [Required(ErrorMessage = "Debe seleccionar la tarjeta de crédito origen.")]
    public int CreditCardId { get; set; }

    [Required(ErrorMessage = "Debe seleccionar la cuenta de ahorro destino.")]
    public int SavingsAccountId { get; set; }

    [Required(ErrorMessage = "El monto del avance es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto del avance debe ser mayor que cero.")]
    public decimal Amount { get; set; }

    public List<SelectListItem> AvailableCards { get; set; } = [];
    public List<SelectListItem> AvailableAccounts { get; set; } = [];
}
```

## `CashAdvanceService`

No hereda del servicio genérico (coordina `CreditCard` + `SavingsAccount` + `Transaction` +
`CardConsumption` + `CashAdvance`).

> **Restricción de arquitectura (obligatoria):** el controlador de la WebApp solo mapea
> `CashAdvanceViewModel → CashAdvanceRequestDto` y llama a `CashAdvanceService.ProcessAsync`.
> No calcula el interés, no valida crédito disponible ni consulta repositorios directamente.
> Ver [`../01-restricciones-tecnicas-transversales.md`](../01-restricciones-tecnicas-transversales.md).

### Dependencias

- `CreditCardRepository`, `SavingsAccountRepository`, `TransactionRepository`,
  `CardConsumptionRepository` (o repositorio genérico de `CardConsumption`).
- `IUnitOfWork` — abre una transacción real de EF Core (`IDbContextTransaction`) para el paso
  2 del procesamiento (acreditar cuenta + aumentar deuda + registrar consumo + registrar
  transacción + registrar `CashAdvance`); si cualquiera falla, se revierten todos.
- `IEmailService` — notificación, fuera de la transacción.
- `ICurrentUserService` — validar que tarjeta y cuenta pertenezcan al cliente autenticado.

### Logging (Serilog)

Log informativo al aprobar o rechazar el avance: `ClientId`, `LastFourDigits` de la tarjeta
(nunca el número completo), monto, interés, total cargado, resultado. Nunca se registra el
CVC, su hash, ni el número completo de tarjeta.

## Correo de avance de efectivo

Enviado **fuera de la transacción**; su fallo no revierte la operación.

Asunto: *"Avance de efectivo desde la tarjeta [XXXX]"*

Cuerpo — incluye como mínimo:

- Monto del avance realizado.
- Interés aplicado.
- Total cargado a la tarjeta.
- Últimos cuatro dígitos de la cuenta de ahorro destino.
- Fecha y hora de la transacción.

Mensaje si falla el envío: **«El avance fue realizado correctamente, pero no fue posible
enviar el correo de notificación.»**

Al finalizar (con o sin éxito de correo), el sistema redirige al cliente al **Home del
cliente**.
