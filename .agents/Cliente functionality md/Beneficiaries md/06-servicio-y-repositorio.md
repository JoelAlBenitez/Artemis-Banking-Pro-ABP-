# Beneficiarios — Servicio y repositorio

> **Restricción de arquitectura (obligatoria):** el controlador solo mapea
> `SaveBeneficiaryViewModel → SaveBeneficiaryDto` y llama a `BeneficiaryService`. Las
> validaciones de existencia/estado de la cuenta y de propiedad viven en el servicio, nunca en
> el controlador. Ver
> [`../01-restricciones-tecnicas-transversales.md`](../01-restricciones-tecnicas-transversales.md).

## `BeneficiaryService`

A diferencia de `TransactionService`/`PaymentService`/`CashAdvanceService`, **sí hereda del
servicio genérico** (`IGenericService<SaveBeneficiaryDto, BeneficiaryDto, Beneficiary, int>`):
es esencialmente CRUD con overrides, sin coordinar transacciones financieras.

### Overrides

| Miembro | Comportamiento |
|---|---|
| `AddAsync` | Validar formato de 9 dígitos → validar existencia y estado activo de la cuenta (`SavingsAccountRepository`) → validar que no sea cuenta propia (`ClientId` de la cuenta ≠ `OwnerClientId` autenticado) → validar que no esté duplicada (`IsActive = true`) → crear `Beneficiary` |
| Método propio: `DeactivateAsync(int id)` | Reemplaza el `DeleteAsync` inexistente: marca `IsActive = false`, `DeactivatedAt = ahora`. Valida que el beneficiario pertenezca al cliente autenticado antes de desactivar |

### Dependencias

- `SavingsAccountRepository` (o `ISavingsAccountQueryService` si se expone como contrato
  cruzado) — para validar existencia/estado de la cuenta a registrar.
- `ICurrentUserService` — para obtener el `ClientId` del cliente autenticado en cada operación
  (nunca confiar en un `ClientId` que venga del formulario).
- Servicios de **Identity** — para resolver nombre y apellido del propietario de la cuenta
  beneficiaria en el listado.

## `BeneficiaryRepository`

Hereda de `GenericRepository`. Override documentado en la base compartida:

| Miembro | Motivo |
|---|---|
| `Query` | Filtro global `IsActive = true`: los beneficiarios dados de baja no deben aparecer nunca salvo consulta explícita |

## Restricción de unicidad

Índice único filtrado: `(OwnerClientId, BeneficiarySavingsAccountId)` donde `IsActive = true`
(definida en Contratos Base, no se redefine aquí).
