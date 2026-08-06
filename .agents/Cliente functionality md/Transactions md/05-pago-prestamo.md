# Pago a préstamo

Pago (abono) desde una cuenta de ahorro propia hacia un préstamo propio activo con cuotas
pendientes.

## Formulario

| Campo | Tipo | Req. | Descripción |
|---|---|---|---|
| Préstamo a pagar | Select | Sí | Solo préstamos **activos** del cliente autenticado |
| Cuenta de origen | Select | Sí | Solo cuentas **activas** del cliente autenticado |
| Monto a pagar | Decimal | Sí | Monto solicitado |

## Validaciones

- Préstamo requerido, propio, **activo**.
- Cuenta de origen requerida, propia, **activa**.
- Monto requerido, **> 0**.
- Fondos suficientes en la cuenta origen para el **monto efectivo**.
- El préstamo debe tener **cuotas pendientes**.

| Situación | Mensaje |
|---|---|
| Fondos insuficientes | «No dispone del monto requerido en la cuenta seleccionada.» |
| Sin cuotas pendientes | «El préstamo seleccionado no tiene cuotas pendientes de pago.» |

## Regla anti-sobrepago (restricción obligatoria, no negociable)

Si el monto ingresado **excede** el monto total pendiente del préstamo, el sistema toma como
monto efectivo únicamente el **pendiente real**. El excedente no se descuenta. Igual que en
el pago a tarjeta, este cálculo se hace en el servicio con el dato real del repositorio, no en
la capa de presentación.

```
montoEfectivo = min(montoSolicitado, montoPendienteReal)
```

## Distribución del pago (`IPaymentAllocator`)

El monto efectivo se aplica a la **cuota pendiente más antigua** primero:

- Si el monto alcanza para completar la cuota → cuota pasa a **Pagada**.
- Si no alcanza → cuota queda **ParcialmentePagada**.
- Si sobra después de saldar una cuota → el excedente continúa aplicándose a la **siguiente**
  cuota pendiente, y así sucesivamente hasta agotar el monto o las cuotas.
- Si **todas** las cuotas quedan pagadas → `Loan.Status = Completado`.
- Si una cuota **atrasada** se paga por completo → su indicador de atraso se revierte
  (`IsOverdue = false`).

## Procesamiento (atómico — `PaymentService.PayLoanAsync`)

1. Debitar `montoEfectivo` de la cuenta origen.
2. `IPaymentAllocator` aplica el monto sobre `LoanInstallment` (de la más antigua a la más
   reciente), actualiza `PaymentStatus`, `PendingBalance` de cada cuota afectada y
   `Loan.PendingAmount`.
3. Registrar `Transaction` **DÉBITO** en la cuenta (`Beneficiary` = número de préstamo),
   `OperationType = PagoPrestamo`, `Channel = Cliente`, estado **APROBADA**.
4. Registrar `LoanPayment` (`LoanId`, `TransactionId`, `RequestedAmount`, `EffectiveAmount`,
   `Channel = Cliente`, `PerformedByUserId`).

Si se rechaza por fondos insuficientes: intento **RECHAZADA** sin afectar balance ni cuotas.

## Correos

Ver [07-correos.md](./07-correos.md).

## Checklist de la rúbrica

- [ ] Pago de préstamo aplicando cuotas en orden de antigüedad.
