# Transferencia entre cuentas propias

## Acceso

- Solo rol **Cliente**. Acceso directo por URL con otro rol → **Acceso denegado**.

## Formulario

| Campo | Tipo | Req. | Descripción |
|---|---|---|---|
| Cuenta de origen | Select | Sí | Cuenta de ahorro **activa** del cliente |
| Cuenta de destino | Select | Sí | Cuenta de ahorro **activa** del **mismo** cliente |
| Monto a transferir | Decimal | Sí | Monto a mover entre las cuentas |

Botón: **Realizar transferencia**.

## Validaciones

- Cuenta de origen y destino requeridas, ambas pertenecientes al cliente autenticado y
  **activas**.
- Origen y destino **no** pueden ser la misma cuenta.
- Monto requerido, **> 0**, no puede exceder el balance disponible de la cuenta origen.
- El cliente debe tener **al menos 2 cuentas de ahorro activas** para poder usar esta
  funcionalidad.

| Situación | Mensaje |
|---|---|
| Menos de 2 cuentas activas | «Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas.» |
| Origen = destino | «La cuenta de origen y la cuenta de destino no pueden ser la misma.» |
| Monto ≤ 0 | «El monto a transferir debe ser mayor que cero.» |
| Fondos insuficientes | «No dispone del monto requerido en la cuenta seleccionada.» |

## Confirmación

Muestra cuenta de origen, cuenta de destino y monto a transferir. Mensaje: **«¿Está seguro
que desea realizar esta transferencia?»** → **Cancelar** (Home) / **Confirmar** (ejecuta).

## Procesamiento (atómico — `TransactionService.ProcessAccountTransferAsync`)

1. Descontar el monto de la cuenta origen; acreditar el mismo monto en la cuenta destino.
2. **Todo o nada**: ambos cambios de balance ocurren dentro de la misma
   `IUnitOfWork.ExecuteInTransactionAsync`, cuya implementación abre una `IDbContextTransaction`
   real de EF Core. Si falla la actualización de alguna cuenta, se hace `Rollback` de ambas —
   nunca puede quedar una cuenta debitada sin su contraparte acreditada.
3. Registrar `Transaction` **DÉBITO** en origen (`Beneficiary` = cuenta destino) y **CRÉDITO**
   en destino (`Origin` = cuenta origen), enlazadas por `RelatedTransactionId`.
4. `OperationType = TransferenciaEntreCuentas`, `Channel = Cliente`. Ambas **APROBADA**.

Si se rechaza (fondos insuficientes u otra validación de negocio): se registra el intento
como **RECHAZADA** en la cuenta de origen, sin afectar ningún balance.

## Correo

Un solo correo, al cliente. Asunto: *"Transferencia entre cuentas realizada"*. Incluye monto,
últimos 4 de origen, últimos 4 de destino, fecha y hora. Enviado **fuera** de la transacción;
su fallo no revierte la operación — mensaje: **«La transferencia fue realizada correctamente,
pero no fue posible enviar el correo de notificación.»**

Al finalizar, redirige al **Home del cliente**.

## Checklist de la rúbrica

- [ ] Transferencia entre cuentas propias con validación de origen y destino distintos.
