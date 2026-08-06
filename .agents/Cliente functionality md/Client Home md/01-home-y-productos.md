# Home del cliente — Menú y productos financieros

## Redirección y acceso

- Tras login exitoso con rol **Cliente**, redirección automática al Home del cliente.
- Solo rol **Cliente** accede. Acceso directo por URL con otro rol → **Acceso denegado**.

## Menú principal

| Opción | Destino |
|---|---|
| Home | Panel principal (productos financieros activos) |
| Transacciones | Menú de operaciones transaccionales |
| Transacciones - Express | Transferencia express hacia cualquier cuenta |
| Transacciones - Tarjeta de crédito | Pago a tarjeta de crédito |
| Transacciones - Préstamo | Pago a préstamo |
| Transacciones - Beneficiarios | Transacción hacia beneficiario registrado |
| Beneficiarios | Administrar beneficiarios |
| Avance de efectivo | Avance desde tarjeta hacia cuenta propia |
| Transferencia | Transferencia entre cuentas propias |
| Cerrar sesión | Cierra sesión y redirige al Login |

## Listado de productos financieros

Se muestran hasta 3 secciones, **solo si aplica**:

| Sección | Condición para mostrarse |
|---|---|
| Cuentas de ahorro | Siempre que tenga ≥ 1 cuenta activa |
| Préstamos | Solo si tiene préstamos activos |
| Tarjetas de crédito | Solo si tiene tarjetas activas |

Si no tiene ningún producto activo: **«No posee productos financieros activos.»**

### Cuentas de ahorro

| Campo | Descripción |
|---|---|
| Número de cuenta | 9 dígitos |
| Balance actual | Monto disponible |
| Tipo de cuenta | Principal / Secundaria |

- La **principal siempre primero**; secundarias ordenadas de **mayor a menor balance**.
- Acción **Ver detalles** → historial de `Transaction` de esa cuenta (solo si pertenece al cliente autenticado), de la más reciente a la más antigua.

### Préstamos (solo si tiene activos)

| Campo | Descripción |
|---|---|
| Número de préstamo, Capital aprobado, Cuotas totales, Cuotas pagadas, Monto pendiente, Tasa anual, Plazo, Estado (**Al día** / **En mora**) |

- **En mora** si tiene ≥ 1 cuota vencida sin saldar completamente.
- Acción **Ver detalles** → tabla de amortización (`LoanInstallment`): fecha de pago, valor de cuota, estado de pago, indicador de atraso.

### Tarjetas de crédito (solo si tiene activas)

| Campo | Descripción |
|---|---|
| Número enmascarado (últimos 4), Límite, Fecha de expiración, Monto adeudado |

- **Nunca** se muestra el número completo.
- Acción **Ver detalles** → historial de `CardConsumption` (más reciente a más antiguo): fecha, monto, comercio (o `AVANCE`), estado (APROBADO/RECHAZADO).

## Reglas adicionales

- El cliente solo visualiza productos que le pertenecen (filtrar siempre por `ClientId` autenticado — usar `ICurrentUserService`).
- Todo botón **Volver atrás** regresa al Home del cliente.
- El Home usa el layout general de la aplicación.

## Checklist de la rúbrica

- [ ] Home del cliente con listado de productos financieros activos.
- [ ] Visualización de detalles de cuentas, préstamos y tarjetas.

> **UI / vistas: No desarrollado por el agente de IA.**
