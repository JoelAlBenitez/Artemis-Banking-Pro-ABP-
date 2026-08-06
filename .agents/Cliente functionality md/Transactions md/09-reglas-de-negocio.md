# Transacciones — Reglas de negocio (resumen)

Reglas adicionales del módulo (documento funcional, pág. 93–94):

- Solo el rol **Cliente** accede a las pantallas de transacciones.
- El cliente solo puede utilizar **cuentas de ahorro activas** que le pertenezcan.
- Todas las transacciones validan **fondos suficientes** antes de afectar balances.
- Todo monto ingresado debe ser **mayor que cero**.
- Las transacciones aprobadas actualizan los balances correspondientes y se registran en el
  historial de las cuentas involucradas.
- Salidas de dinero = **DÉBITO**; entradas = **CRÉDITO**.
- Los pagos a tarjetas y préstamos se registran como **DÉBITO** en la cuenta de ahorro
  origen.
- Los pagos a tarjetas **no** pueden exceder la deuda actual de la tarjeta.
- Los pagos a préstamos **no** pueden exceder el monto pendiente real del préstamo.
- El sistema **no** descuenta excedentes que no puedan aplicarse a una tarjeta o préstamo.
- Las transacciones rechazadas **no** modifican balances ni deudas.
- Las operaciones confirmadas **no** se revierten por errores en el envío de correos.
- Al finalizar cualquier operación, el sistema redirige al cliente al **Home del cliente**.

## Checklist de la rúbrica (Funcionalidades del cliente — Transacciones)

- [ ] Transacción Express con validación de cuenta destino y fondos.
- [ ] Pago de tarjeta de crédito desde cuenta propia sin permitir sobrepago.
- [ ] Pago de préstamo aplicando cuotas en orden de antigüedad.
- [ ] Transacción a beneficiarios con registro de débito y crédito.
- [ ] Correos de notificación y registros de historial en operaciones del cliente.

## Reglas transversales del sistema aplicadas aquí

- No borrado físico (`Transaction` es inmutable).
- Atomicidad vía `IUnitOfWork` en cada operación compuesta.
- AutoMapper como única frontera de conversión.
- Correos siempre fuera de la transacción.
