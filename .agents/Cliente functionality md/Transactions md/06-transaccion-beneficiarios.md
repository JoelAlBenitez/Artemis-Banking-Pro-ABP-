# Transacción a beneficiarios

Transfiere fondos desde una cuenta propia activa hacia una cuenta previamente registrada
como **beneficiario** del cliente autenticado.

## Formulario

| Campo | Tipo | Req. | Descripción |
|---|---|---|---|
| Beneficiario | Select | Sí | Beneficiario registrado por el cliente autenticado (muestra número de cuenta + nombre) |
| Monto a transferir | Decimal | Sí | Monto a enviar |
| Cuenta de origen | Select | Sí | Solo cuentas **activas** del cliente autenticado |

## Validaciones

- Beneficiario requerido, debe pertenecer al cliente autenticado.
- La cuenta del beneficiario debe **existir** y estar **activa**.
- Monto requerido, **> 0**.
- Cuenta de origen requerida, propia, **activa**, con **fondos suficientes**.

| Situación | Mensaje |
|---|---|
| Sin beneficiarios | «No tiene beneficiarios registrados.» |
| Cuenta del beneficiario no disponible | «La cuenta del beneficiario no se encuentra disponible.» |
| Fondos insuficientes | «No dispone de fondos suficientes para realizar esta transacción.» |

## Confirmación

Muestra nombre/apellido del titular beneficiario, número de cuenta y monto. Mensaje: **«¿Está
seguro de que desea realizar esta transacción?»** → **Cancelar** (Home) / **Confirmar**
(ejecuta).

## Procesamiento (atómico — `TransactionService.ProcessBeneficiaryTransactionAsync`)

1. Debitar cuenta origen, acreditar cuenta del beneficiario.
2. Registrar `Transaction` **DÉBITO** en origen (`Beneficiary` = cuenta del beneficiario) y
   **CRÉDITO** en la cuenta del beneficiario (`Origin` = cuenta origen), enlazadas por
   `RelatedTransactionId`.
3. `OperationType = TransaccionBeneficiario`, `Channel = Cliente`. Ambas **APROBADA**.

Si se rechaza por fondos insuficientes: intento **RECHAZADA** en la cuenta de origen, sin
afectar balances.

## Correos

Ver [07-correos.md](./07-correos.md) — igual que Express: dos correos (emisor y receptor).

## Checklist de la rúbrica

- [ ] Transacción a beneficiarios con registro de débito y crédito.
