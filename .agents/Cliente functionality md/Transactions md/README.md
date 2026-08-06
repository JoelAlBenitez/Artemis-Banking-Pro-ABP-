# Transacciones del cliente

Cuatro pantallas: Transacción Express, Pago a tarjeta de crédito, Pago a préstamo y
Transacción a beneficiarios. Fuente: documento funcional, páginas 81–94.

## Contenido

| Archivo | Tema |
|---|---|
| [01 - Overview y acceso](./01-overview-y-acceso.md) | Alcance común a las 4 pantallas |
| [02 - DTOs y ViewModels](./02-dtos-y-viewmodels.md) | Contratos de cada pantalla |
| [03 - Transacción Express](./03-transaccion-express.md) | Cuenta propia → cualquier cuenta destino |
| [04 - Pago a tarjeta de crédito](./04-pago-tarjeta-credito.md) | Cuenta propia → tarjeta propia, anti-sobrepago |
| [05 - Pago a préstamo](./05-pago-prestamo.md) | Cuenta propia → préstamo propio, orden de antigüedad |
| [06 - Transacción a beneficiarios](./06-transaccion-beneficiarios.md) | Cuenta propia → beneficiario registrado |
| [07 - Correos](./07-correos.md) | Notificaciones de las 4 operaciones |
| [08 - Servicio y repositorio](./08-servicio-y-repositorio.md) | `TransactionService`, `PaymentService`, contrato externo |
| [09 - Reglas de negocio](./09-reglas-de-negocio.md) | Resumen y checklist de la rúbrica |

## Entidad central (referencia — ya definida en Contratos Base)

`Transaction`: `SavingsAccountId`, `Amount` (>0), `TransactionType` (Débito/Crédito),
`OperationType`, `Origin`, `Beneficiary`, `Status` (Aprobada/Rechazada), `RejectionReason?`,
`PerformedByUserId`, `Channel` (`PaymentChannel.Cliente`), `RelatedTransactionId?` (enlaza el
par débito/crédito). **Inmutable.**

## Reglas clave de un vistazo

- El cliente solo opera sobre **cuentas, tarjetas y préstamos propios y activos**.
- Todo monto ingresado debe ser **mayor que cero**.
- Salidas de dinero = **DÉBITO**; entradas = **CRÉDITO**.
- Pagos (tarjeta/préstamo) **nunca exceden** la deuda/pendiente real: se cobra el monto
  efectivo, el excedente no se descuenta.
- Toda operación aprobada se registra; los **rechazos también se persisten**, sin afectar
  balances.
- Toda operación pasa por una pantalla de **confirmación** antes de ejecutarse (excepto pago
  a tarjeta/préstamo, que valida y ejecuta directo según el funcional).
- Correos **siempre fuera de la transacción**.

> Base compartida relacionada: `Shared base md/` (del otro integrante). **UI / vistas y
> pruebas: No desarrollado por el agente de IA.**
