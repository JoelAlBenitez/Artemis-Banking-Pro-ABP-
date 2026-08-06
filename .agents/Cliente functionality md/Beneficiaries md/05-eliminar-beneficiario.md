# Beneficiarios — Eliminar (baja lógica)

Cada beneficiario del listado tiene una acción **Eliminar**.

## Confirmación

Mensaje: **«¿Está seguro que desea eliminar este beneficiario?»**

| Botón | Efecto |
|---|---|
| Cancelar | Cierra la confirmación sin cambios |
| Aceptar | Elimina el beneficiario del listado del cliente autenticado |

## Efecto real

- **No es un borrado físico.** Se marca `IsActive = false` y `DeactivatedAt = ahora`
  (`ISoftDeletable`), vía el método propio del servicio (no `DeleteAsync`, que no existe).
- **No** elimina la cuenta de ahorro asociada.
- **No** afecta el historial de transacciones realizadas previamente.
- Solo afecta la **relación** entre el cliente autenticado y esa cuenta beneficiaria.

Mensaje tras confirmar: **«Beneficiario eliminado correctamente.»**

## Checklist de la rúbrica

- [ ] Eliminación de beneficiarios sin afectar historial ni cuentas.

> **UI / vistas: No desarrollado por el agente de IA.**
