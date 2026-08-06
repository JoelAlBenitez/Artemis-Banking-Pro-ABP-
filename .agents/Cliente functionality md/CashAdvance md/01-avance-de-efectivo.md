# Avance de efectivo

## Acceso

- Solo rol **Cliente**. Acceso directo por URL con otro rol → **Acceso denegado**.

## Formulario

| Campo | Tipo | Req. | Descripción |
|---|---|---|---|
| Tarjeta de crédito origen | Select | Sí | Solo tarjetas **activas** del cliente autenticado |
| Cuenta de ahorro destino | Select | Sí | Solo cuentas **activas** del cliente autenticado |
| Monto del avance de efectivo | Decimal | Sí | Monto que el cliente recibirá en su cuenta (no incluye interés) |

Botón: **Realizar avance**.

## Validaciones

- Tarjeta requerida, propia, **activa** y **no vencida**.
- Cuenta destino requerida, propia, **activa**.
- Monto requerido, **> 0**.
- La tarjeta debe tener **crédito disponible suficiente** para cubrir monto + interés.

| Situación | Mensaje |
|---|---|
| Tarjeta no activa | «La tarjeta seleccionada no se encuentra activa.» |
| Tarjeta vencida | «La tarjeta seleccionada se encuentra vencida.» |
| Cuenta destino no activa | «La cuenta de ahorro seleccionada no se encuentra activa.» |
| Monto ≤ 0 | «El monto del avance debe ser mayor que cero.» |
| Crédito insuficiente | «El avance solicitado excede el crédito disponible de la tarjeta seleccionada.» |

## Cálculo del crédito disponible e interés

```
creditoDisponible = CreditCard.CreditLimit - CreditCard.OwedAmount
interes           = montoAvance * 0.0625        // 6.25 %
totalACargar      = montoAvance + interes

// La operación se aprueba SOLO si:
totalACargar <= creditoDisponible
```

**Ejemplo (del funcional):** tarjeta con límite RD$500.00 y deuda RD$300.00 → disponible
RD$200.00. Avance solicitado RD$200.00 → interés RD$12.50 → total a cargar RD$212.50 →
**rechazado** (supera el disponible).

**Ejemplo de aprobación:** avance de RD$100.00 → interés RD$6.25 → cuenta recibe RD$100.00 →
tarjeta aumenta su deuda en RD$106.25.

## Procesamiento (atómico — `CashAdvanceService.ProcessAsync`)

Ver el pseudocódigo completo de referencia en Contratos Base, sección 11.3 (`CashAdvanceService.ProcessAsync`). Resumen:

1. **Validar antes de abrir transacción**: si `totalACargar > creditoDisponible`, registrar el
   intento como `CardConsumption` **RECHAZADO** (`RejectionReason = CreditoInsuficiente`) —
   **fuera** de la unidad de trabajo principal — y devolver el mensaje de rechazo.
2. Si es válido, dentro de `IUnitOfWork.ExecuteInTransactionAsync`:
   - Acreditar `montoAvance` (solo el monto, **sin** interés) en `SavingsAccount.Balance`.
   - Aumentar `CreditCard.OwedAmount` en `totalACargar` (monto + interés).
   - Registrar `CardConsumption` **APROBADO**, `Origin = Avance`, `CommerceName = "AVANCE"`,
     `Amount = totalACargar`.
   - Registrar `Transaction` **CRÉDITO** en la cuenta destino, `OperationType =
     AvanceEfectivo`, `Origin` = últimos 4 dígitos de la tarjeta, `Channel = Cliente`.
   - Registrar `CashAdvance` (enlaza `CardConsumptionId` y `TransactionId`).
3. Enviar correo **fuera** de la transacción (ver [02](./02-dtos-y-servicio.md)).

## Checklist de la rúbrica

- [ ] Avance de efectivo con interés del 6.25% y validación de crédito disponible.
