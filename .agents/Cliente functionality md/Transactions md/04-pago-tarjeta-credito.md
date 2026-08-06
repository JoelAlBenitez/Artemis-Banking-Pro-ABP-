# Pago a tarjeta de crédito

Pago desde una cuenta de ahorro propia hacia una tarjeta de crédito propia con deuda
pendiente.

## Formulario

| Campo | Tipo | Req. | Descripción |
|---|---|---|---|
| Tarjeta de crédito destino | Select | Sí | Solo tarjetas **activas** del cliente autenticado |
| Cuenta de origen | Select | Sí | Solo cuentas **activas** del cliente autenticado |
| Monto a pagar | Decimal | Sí | Monto solicitado por el cliente |

## Validaciones

- Tarjeta requerida, debe pertenecer al cliente autenticado y estar **activa**.
- Cuenta de origen requerida, propia y **activa**.
- Monto requerido, **> 0**.
- La cuenta de origen debe tener fondos suficientes para el **monto efectivo**.
- La tarjeta debe tener **deuda pendiente**.

| Situación | Mensaje |
|---|---|
| Fondos insuficientes | «No dispone del monto requerido en la cuenta seleccionada.» |
| Sin deuda pendiente | «La tarjeta seleccionada no tiene deuda pendiente.» |

## Regla anti-sobrepago (restricción obligatoria, no negociable)

Si el monto ingresado **excede** la deuda actual de la tarjeta, el sistema **no** descuenta el
monto completo: toma como monto efectivo únicamente el valor de la deuda actual. El
excedente **no se descuenta ni se utiliza**. Esta validación se calcula en el servicio, con el
dato de deuda leído del repositorio — nunca se confía en un "monto efectivo" calculado en el
ViewModel o en la vista.

```
montoEfectivo = min(montoSolicitado, deudaActualTarjeta)
```

## Procesamiento (atómico — `PaymentService.PayCreditCardAsync`)

1. Debitar `montoEfectivo` de la cuenta origen.
2. Reducir `CreditCard.OwedAmount` en `montoEfectivo`; recalcular `AvailableCredit` (CALC).
3. Registrar `Transaction` **DÉBITO** en la cuenta (`Beneficiary` = últimos 4 dígitos de la
   tarjeta, `Origin` = cuenta origen), `OperationType = PagoTarjeta`, `Channel = Cliente`,
   estado **APROBADA**.
4. Registrar `CardPayment` (`CreditCardId`, `TransactionId`, `RequestedAmount`,
   `EffectiveAmount`, `Channel = Cliente`, `PerformedByUserId`).

Si se rechaza por fondos insuficientes: se registra el intento como **RECHAZADA** sin afectar
balance ni deuda.

## Correos

Ver [07-correos.md](./07-correos.md).

## Checklist de la rúbrica

- [ ] Pago de tarjeta de crédito desde cuenta propia sin permitir sobrepago.
