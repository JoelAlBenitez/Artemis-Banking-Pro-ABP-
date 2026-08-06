# Home del cliente

Panel principal del cliente: menú de navegación y listado de productos financieros activos.
Fuente: documento funcional, páginas 69–77.

## Contenido

| Archivo | Tema |
|---|---|
| [01 - Home y productos financieros](./01-home-y-productos.md) | Menú, listado de cuentas/préstamos/tarjetas, ver detalles |

## Resumen

- Solo accesible para rol **Cliente**. Acceso directo por URL con otro rol → **Acceso denegado**.
- Muestra únicamente productos **activos**; nunca datos sensibles completos (tarjeta, CVC).
- Usa `ClientHomeViewModel` (ver inventario en Contratos Base, sección 7.3).

> **UI / vistas: No desarrollado por el agente de IA.**
