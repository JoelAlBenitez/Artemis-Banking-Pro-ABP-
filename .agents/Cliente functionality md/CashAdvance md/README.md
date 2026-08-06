# Avance de efectivo

Transferencia de fondos desde una tarjeta de crédito propia hacia una cuenta de ahorro
propia, con interés del 6.25 %. Fuente: documento funcional, páginas 94–100.

## Contenido

| Archivo | Tema |
|---|---|
| [01 - Avance de efectivo](./01-avance-de-efectivo.md) | Formulario, validaciones, cálculo de interés y procesamiento |
| [02 - DTOs, servicio y correo](./02-dtos-y-servicio.md) | Contratos, `CashAdvanceService`, notificación |
| [03 - Reglas de negocio](./03-reglas-de-negocio.md) | Resumen y checklist de la rúbrica |

## Entidad de trazabilidad (referencia — ya definida en Contratos Base)

`CashAdvance`: `CreditCardId`, `SavingsAccountId`, `RequestedAmount` (sin interés),
`InterestRate` (6.25 % al momento de la operación), `InterestAmount`, `TotalCharged`
(monto + interés), `CardConsumptionId` (FK), `TransactionId` (FK — el CRÉDITO en la cuenta
destino).

## Fórmula clave

```
InterestRate      = DomainConstants.CashAdvanceInterestRate  // 0.0625m (6.25 %)
InterestAmount     = RequestedAmount * InterestRate
TotalCharged        = RequestedAmount + InterestAmount
```

`TotalCharged` no puede superar el **crédito disponible** de la tarjeta
(`CreditLimit - OwedAmount`).

> Base compartida relacionada: `Shared base md/` (del otro integrante). El ejemplo completo
> de `CashAdvanceService.ProcessAsync` ya está codificado en Contratos Base, sección 11.3 —
> ese es el punto de partida real para la implementación. **UI / vistas y pruebas: No
> desarrollado por el agente de IA.**
