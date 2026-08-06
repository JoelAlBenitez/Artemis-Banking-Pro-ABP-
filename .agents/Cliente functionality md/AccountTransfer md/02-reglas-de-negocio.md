# Transferencia entre cuentas — Reglas de negocio (resumen)

Reglas adicionales del módulo (documento funcional, pág. 104–105):

- Solo el rol **Cliente** accede a la funcionalidad de Transferencia entre cuentas.
- El cliente solo puede transferir fondos entre cuentas de ahorro que le **pertenezcan**.
- Solo se muestran cuentas de ahorro **activas** en los selectores.
- La cuenta de origen y la cuenta de destino **no** pueden ser la misma cuenta.
- El monto a transferir debe ser **mayor que cero**.
- La cuenta de origen debe tener **fondos suficientes** antes de aprobar la transferencia.
- La cuenta de origen registra la operación como **DÉBITO**.
- La cuenta de destino registra la operación como **CRÉDITO**.
- Las transferencias **aprobadas** actualizan los balances de ambas cuentas.
- Las transferencias **rechazadas** no modifican balances.
- La operación se ejecuta de forma **transaccional** para evitar inconsistencias (todo o
  nada).
- Al finalizar, el sistema redirige al cliente al **Home del cliente**.

## Checklist de la rúbrica (Funcionalidades del cliente — Transferencia entre cuentas)

- [ ] Transferencia entre cuentas propias con validación de origen y destino distintos.
- [ ] Correos de notificación y registros de historial en operaciones del cliente.
