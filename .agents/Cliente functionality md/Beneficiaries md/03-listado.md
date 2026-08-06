# Beneficiarios — Listado

En la pantalla inicial se muestra el listado de **todos** los beneficiarios registrados por
el cliente **autenticado** (`OwnerClientId` filtrado, `IsActive = true`).

## Columnas por beneficiario

| Campo | Descripción |
|---|---|
| Nombre | Nombre del propietario de la cuenta beneficiaria |
| Apellido | Apellido del propietario de la cuenta beneficiaria |
| Número de cuenta | Identificador de la cuenta de ahorro registrada como beneficiaria |

## Acciones

| Acción | Descripción |
|---|---|
| Eliminar | Quita el beneficiario del listado del cliente autenticado → [05](./05-eliminar-beneficiario.md) |

Arriba del listado: botón **Agregar beneficiario** → [04](./04-agregar-beneficiario.md).

> No especifica paginación explícita para este listado en el documento funcional (a
> diferencia de los listados administrativos, que sí exigen máx. 20/página). **No
> especificado** → se recomienda aplicar el mismo estándar de paginación (`PagedViewModel<T>`)
> por consistencia, salvo indicación en contra del equipo.

> **UI / vistas: No desarrollado por el agente de IA.**
