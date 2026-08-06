# Beneficiarios — Agregar beneficiario

Botón **Agregar beneficiario** → modal o pantalla con `SaveBeneficiaryViewModel`.

## Formulario

| Campo | Tipo | Req. | Descripción |
|---|---|---|---|
| Número de cuenta | Texto / string | Sí | Cuenta de ahorro que se desea registrar como beneficiaria |

Botones: **Cancelar** (cierra sin guardar) y **Guardar** (valida y registra).

## Validaciones

- El número de cuenta es requerido.
- Debe contener exactamente **9 dígitos**.
- Debe **existir** en el sistema.
- La cuenta debe estar **activa**.
- La cuenta **no** debe pertenecer al cliente autenticado.
- La cuenta **no** debe estar ya registrada como beneficiario del cliente autenticado.

| Situación | Mensaje |
|---|---|
| No existe | «El número de cuenta ingresado no corresponde a una cuenta válida.» |
| Cancelada | «No puede agregar una cuenta cancelada como beneficiario.» |
| Cuenta propia | «No puede agregar una cuenta propia como beneficiario. Utilice la opción Transferencia para mover fondos entre sus cuentas.» |
| Ya registrada | «Esta cuenta ya se encuentra registrada como beneficiario.» |

## Registro

Si todas las validaciones son correctas:

- Se crea el `Beneficiary` asociado al `OwnerClientId` autenticado.
- Nombre y apellido **no se persisten**: se resuelven desde Identity a partir del propietario
  de la cuenta.
- Mensaje: **«Beneficiario agregado correctamente.»**

> No es una operación compuesta (no toca `Transaction` ni balances): no requiere
> `IUnitOfWork.ExecuteInTransactionAsync`, solo el `SaveChangesAsync` estándar del servicio
> genérico.

## Checklist de la rúbrica

- [ ] Gestión de beneficiarios con validación de cuenta activa y no propia.

> **UI / vistas: No desarrollado por el agente de IA.**
