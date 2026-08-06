# Restricciones técnicas transversales (obligatorias)

Estas reglas aplican a **las 5 carpetas** de este módulo (`Client Home`, `Beneficiaries`,
`Transactions`, `CashAdvance`, `AccountTransfer`). No son opcionales: son requisitos técnicos
del documento funcional y del documento de Entidades y Contratos Base que deben respetarse en
cada archivo de servicio/repositorio ya escrito.

## 1 · Onion Architecture — los controladores nunca contienen lógica

- El controlador de la WebApp **solo** recibe el ViewModel, lo mapea a `SaveDto` y llama al
  método del servicio correspondiente (`TransactionService`, `PaymentService`,
  `CashAdvanceService`, `BeneficiaryService`). **Nunca** consulta un repositorio directamente
  ni contiene una regla de negocio (validación de fondos, cálculo de interés, orden de
  aplicación de cuotas, etc.).
- Toda regla financiera vive en la capa de aplicación (servicio) o en un servicio de dominio
  (`IPaymentAllocator`, `IDebtCalculator`, etc.), nunca en `Presentation.WebApp`.
- Aplica a: `Transactions md/08-servicio-y-repositorio.md`,
  `CashAdvance md/02-dtos-y-servicio.md`, `Beneficiaries md/06-servicio-y-repositorio.md`.

## 2 · Atomicidad real con transacciones de Entity Framework Core

- Toda operación que modifica **más de un balance o más de una entidad relacionada**
  (Transferencia, Express, transacción a beneficiario, pago a tarjeta, pago a préstamo, avance
  de efectivo) se ejecuta dentro de `IUnitOfWork.ExecuteInTransactionAsync`, cuya
  implementación real abre una `IDbContextTransaction` de EF Core
  (`_context.Database.BeginTransactionAsync()`), hace `Commit` si todo tiene éxito y
  `Rollback` ante cualquier excepción — nunca se aplican débito y crédito por separado con dos
  `SaveChangesAsync` independientes.
- Si falla el paso de acreditar la cuenta destino después de debitar la de origen, **ambos**
  cambios se revierten: no puede quedar una cuenta debitada sin su contraparte acreditada.
- Ya documentado explícitamente en `AccountTransfer md/01-transferencia-entre-cuentas.md` y
  `CashAdvance md/01-avance-de-efectivo.md`; el mismo criterio aplica sin excepción a los 6
  flujos de `Transactions md/`.

## 3 · Anti-sobrepago — restricción no negociable

- En **pago a tarjeta** y **pago a préstamo**, si el monto ingresado por el cliente supera la
  deuda/pendiente real, el sistema **jamás** debita el monto completo: calcula
  `montoEfectivo = min(montoSolicitado, deudaOPendienteReal)` **antes** de tocar cualquier
  balance, y el excedente se descarta sin registrar movimiento adicional.
- Esta regla se valida en el servicio de aplicación (`PaymentService`), no en el ViewModel ni
  en la vista, porque requiere leer el dato de deuda/pendiente actual desde el repositorio.
- Detalle completo en `Transactions md/04-pago-tarjeta-credito.md` y
  `Transactions md/05-pago-prestamo.md`.

## 4 · Serilog — datos prohibidos en logs

Toda operación financiera de este módulo (Express, pago a tarjeta, pago a préstamo,
transacción a beneficiario, avance de efectivo, transferencia) debe generar un log
**informativo** en Serilog al completarse, y un log de **error** si la operación fue
rechazada o si ocurrió una excepción no controlada — pero **nunca** debe registrarse:

- Número completo de tarjeta (`CardNumber`) ni su `CvcHash`.
- Contraseñas, tokens JWT completos, tokens de activación/restablecimiento.
- Cadenas de conexión o secretos de configuración.

Para identificar una tarjeta en un log se usa **únicamente** `LastFourDigits`. El log debe
incluir: fecha/hora, usuario y rol autenticado (`ICurrentUserService`), acción ejecutada,
identificador de correlación y resultado (aprobado/rechazado). Esta regla ya está definida de
forma transversal en la base compartida (`Shared base md/10-servicios-de-dominio.md`); aquí se
recalca porque los 6 flujos de este módulo son, junto con los del Cajero, los que generan el
mayor volumen de logs financieros del sistema.

## 5 · Pruebas unitarias (xUnit) — fuera del alcance de este agente

El documento de contexto del proyecto excluye explícitamente **pruebas unitarias y de
integración** del trabajo que realiza este agente de IA («No desarrollado por el agente de
IA — implementación realizada manualmente»). Por lo tanto:

- **No** se generan aquí pruebas xUnit para el cálculo del 6.25 % del avance ni para la
  aplicación de abonos por antigüedad de cuota.
- Estas fórmulas están completamente especificadas (ver
  `CashAdvance md/01-avance-de-efectivo.md` y `Transactions md/05-pago-prestamo.md`) para que
  **tú** —u otro agente fuera de este alcance acordado— puedas escribir esas pruebas
  manualmente si el equipo lo decide así.
- Si el alcance del proyecto cambia y sí quieres que se generen pruebas unitarias, dímelo
  explícitamente y lo tratamos como una tarea aparte — no lo voy a asumir por defecto porque
  contradice la instrucción original de exclusión.

## 6 · Escalabilidad

Aunque el documento funcional no dedica una sección propia a "escalabilidad", varios
requisitos técnicos ya definidos en la base compartida existen precisamente para que el
sistema escale sin rediseño:

- **Todo el acceso a datos es asíncrono** (`Task`/`async`/`await` en repositorios y
  servicios) — evita bloquear hilos bajo carga concurrente, algo crítico en operaciones
  financieras donde múltiples clientes transaccionan a la vez.
- **Paginación obligatoria en todo listado** (máx. 20 registros, `GetPagedAsync`) — ningún
  endpoint ni vista de este módulo puede traer un `GetAllAsync` sin límite; los listados de
  transacciones y consumos crecen indefinidamente en producción.
- **`ProjectTo<TDto>()` / `IQueryable` hasta el final de la consulta** — el filtrado, la
  paginación y la proyección a DTO se resuelven en SQL, no en memoria, evitando traer
  columnas o filas innecesarias (incluidas las sensibles).
- **Servicios sin estado (stateless)** — `TransactionService`, `PaymentService` y
  `CashAdvanceService` no guardan estado entre llamadas; toda la información de la operación
  viaja en el DTO de entrada. Esto permite escalar horizontalmente la Web App/Web API sin
  afinidad de sesión para estas operaciones.
- **Índices únicos filtrados** (p. ej. `(OwnerClientId, BeneficiarySavingsAccountId)` con
  `IsActive = true`) — sostienen la integridad sin bloqueos adicionales en tablas que crecen
  rápido, como `Beneficiary` y `Transaction`.
- **`Transaction` como historial append-only** — al ser inmutable y nunca actualizarse ni
  borrarse, no genera contención de escritura sobre filas existentes; cada operación solo
  inserta filas nuevas, patrón favorable para el volumen alto que maneja este módulo.

Si el equipo requiere métricas de escalabilidad más específicas (límites de throughput,
particionamiento de la tabla `Transaction`, colas para el envío de correos), eso está **fuera**
de lo que el documento funcional especifica hoy — se marcaría como **No especificado** hasta
que el equipo lo defina.
