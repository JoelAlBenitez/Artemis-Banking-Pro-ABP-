# Transferencia entre cuentas propias

Movimiento de fondos entre dos cuentas de ahorro **propias** del cliente autenticado.
Fuente: documento funcional, páginas 100–105.

## Contenido

| Archivo | Tema |
|---|---|
| [01 - Transferencia entre cuentas](./01-transferencia-entre-cuentas.md) | Formulario, validaciones, procesamiento |
| [02 - Reglas de negocio](./02-reglas-de-negocio.md) | Resumen y checklist de la rúbrica |

## Diferencia con Transacción Express

| | Transferencia entre cuentas | Transacción Express |
|---|---|---|
| Cuenta destino | **Propia** (select) | **Ajena** (número manual) |
| Requisito previo | Cliente con **≥ 2 cuentas activas** | Ninguno |
| Confirmación | Sí | Sí |
| Correos | 1 (al mismo cliente) | 2 (emisor y receptor) |

> Base compartida relacionada: `Shared base md/` (del otro integrante). **UI / vistas y
> pruebas: No desarrollado por el agente de IA.**
