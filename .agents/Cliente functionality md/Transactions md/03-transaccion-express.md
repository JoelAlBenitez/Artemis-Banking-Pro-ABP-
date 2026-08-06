# Transacción Express

Envía dinero desde una cuenta propia activa hacia **cualquier** cuenta de ahorro activa del
sistema, ingresando manualmente el número de cuenta destino.

## Formulario

| Campo | Tipo | Req. | Descripción |
|---|---|---|---|
| Número de cuenta destino | Texto | Sí | Cuenta a la que se desea transferir |
| Monto a transferir | Decimal | Sí | Monto a enviar |
| Cuenta de origen | Select | Sí | Solo cuentas **activas** del cliente autenticado |

## Validaciones

- Número de cuenta destino requerido, debe **existir** y estar **activa**.
- Monto requerido, **> 0**.
- Cuenta de origen requerida, debe pertenecer al cliente autenticado y estar **activa**.
- La cuenta de origen debe tener **fondos suficientes**.
- Origen y destino **no** pueden ser la misma cuenta.

| Situación | Mensaje |
|---|---|
| Destino inválido/inactiva | «El número de cuenta ingresado no corresponde a una cuenta válida.» |
| Fondos insuficientes | «El monto ingresado excede el saldo disponible de la cuenta seleccionada.» |
| Origen = destino | «La cuenta destino no puede ser la misma cuenta de origen.» |

## Confirmación

Muestra nombre/apellido del titular destino, número de cuenta destino y monto.
Mensaje: **«¿Está seguro de que desea realizar esta transacción?»** → **Cancelar** (regresa
al Home) / **Confirmar** (ejecuta).

## Procesamiento (atómico — `TransactionService.ProcessExpressAsync`)

1. Debitar cuenta origen, acreditar cuenta destino.
2. Registrar `Transaction` **DÉBITO** en origen (`Beneficiary` = cuenta destino) y **CRÉDITO**
   en destino (`Origin` = cuenta origen), enlazadas por `RelatedTransactionId`.
3. `OperationType = TransaccionExpress`, `Channel = Cliente`.
4. Ambas quedan **APROBADA**.

Si se rechaza por fondos insuficientes: se registra el intento como **RECHAZADA** en la
cuenta de origen (`RejectionReason = FondosInsuficientes`), sin afectar balances.

## Correos

Ver [07-correos.md](./07-correos.md) — dos correos (emisor y receptor), fuera de la
transacción.

## Checklist de la rúbrica

- [ ] Transacción Express con validación de cuenta destino y fondos.
