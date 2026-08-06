# Gestión de beneficiarios

Módulo del cliente para registrar cuentas de ahorro de otros clientes como beneficiarios
frecuentes. Fuente: documento funcional, páginas 77–81.

## Contenido

| Archivo | Tema |
|---|---|
| [01 - Overview y acceso](./01-overview-y-acceso.md) | Alcance, acceso por rol |
| [02 - DTOs y ViewModels](./02-dtos-y-viewmodels.md) | Contratos de escritura y lectura |
| [03 - Listado](./03-listado.md) | Listado de beneficiarios del cliente autenticado |
| [04 - Agregar beneficiario](./04-agregar-beneficiario.md) | Formulario y validaciones |
| [05 - Eliminar beneficiario](./05-eliminar-beneficiario.md) | Baja lógica (`IsActive = false`) |
| [06 - Servicio y repositorio](./06-servicio-y-repositorio.md) | `BeneficiaryService`, overrides |
| [07 - Reglas de negocio](./07-reglas-de-negocio.md) | Resumen y checklist de la rúbrica |

## Entidad (referencia — ya definida en Contratos Base, no se redefine)

`Beneficiary`: `OwnerClientId`, `BeneficiarySavingsAccountId` (FK), `BeneficiaryAccountNumber`
(copia desnormalizada), `IsActive` (`ISoftDeletable`), `DeactivatedAt`.

- Índice único filtrado: `(OwnerClientId, BeneficiarySavingsAccountId)` donde `IsActive = true`.
- El nombre y apellido del beneficiario **no se persisten**: se resuelven en tiempo de
  consulta desde Identity a partir del `ClientId` dueño de la cuenta beneficiaria.

## Reglas clave de un vistazo

- Un beneficiario debe ser una cuenta de ahorro **activa** y **existente**.
- No se puede registrar una **cuenta propia** ni una **cuenta cancelada**.
- No se puede duplicar la misma cuenta como beneficiario del mismo cliente.
- **Eliminar** = `IsActive = false`. No borra la cuenta ni el historial de transacciones.

> Base compartida relacionada: `Shared base md/` (del otro integrante). **UI / vistas y
> pruebas: No desarrollado por el agente de IA.**
