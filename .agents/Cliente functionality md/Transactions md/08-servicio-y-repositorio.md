# Transacciones — Servicio, repositorio y contrato externo

> **Restricción de arquitectura (obligatoria):** los controladores de la WebApp reciben el
> ViewModel, lo mapean a `SaveDto` y llaman al método del servicio correspondiente. Ningún
> controlador consulta un repositorio directamente ni contiene lógica de validación de
> fondos, cálculo de monto efectivo o clasificación de `OperationType` — toda esa lógica vive
> en `TransactionService`/`PaymentService`. Ver
> [`../01-restricciones-tecnicas-transversales.md`](../01-restricciones-tecnicas-transversales.md).

## Servicios (no heredan del genérico)

Según Contratos Base, sección 9.3: *"Las operaciones puramente transaccionales —
`TransactionService`, `PaymentService`, `CashAdvanceService` — no son CRUD sobre una entidad:
son casos de uso que coordinan varias entidades. Se implementan como servicios de aplicación
independientes con métodos propios de intención, siempre dentro de una unidad de trabajo
explícita."*

### `TransactionService`

| Método | Cubre |
|---|---|
| `ProcessExpressAsync(ExpressTransactionDto)` | Transacción Express |
| `ProcessBeneficiaryTransactionAsync(BeneficiaryTransactionDto)` | Transacción a beneficiarios |
| `GetTotalHistoricalAsync()` | Contrato externo — ver abajo |
| `GetTotalTodayAsync()` | Contrato externo — ver abajo |
| `RegisterInitialTransactionAsync(...)` | Contrato externo — ver abajo |

### `PaymentService`

| Método | Cubre |
|---|---|
| `PayCreditCardAsync(PayCreditCardDto)` | Pago a tarjeta de crédito |
| `PayLoanAsync(PayLoanDto)` | Pago a préstamo |

### Dependencias comunes

- `IUnitOfWork` — atomicidad de cada operación.
- `IEmailService` — notificaciones, siempre fuera de la transacción.
- `ICurrentUserService` — `ClientId` del cliente autenticado.
- `IPaymentAllocator` — solo para `PayLoanAsync`.
- Repositorios: `SavingsAccountRepository`, `CreditCardRepository`, `LoanRepository`,
  `TransactionRepository`, `BeneficiaryRepository`.
- Servicios de **Identity** — datos del titular de cuentas/beneficiarios.

## `TransactionRepository`

Hereda de `GenericRepository`. Métodos propios (definidos en Contratos Base, para los
indicadores del Dashboard de Administrador y del Home del cajero):

| Miembro | Uso |
|---|---|
| `GetTotalHistoricalAsync()` | Total de transacciones desde el inicio del sistema, sin filtros |
| `GetTotalTodayAsync()` | Total de transacciones de la fecha actual |
| `GetPaymentsAsync(channel, date)` | Pagos (`OperationType` ∈ {PagoTarjeta, PagoPrestamo}, `Status = Aprobada`) — lo usa Administrador y Cajero |

## Contrato externo que este módulo debe exponer (Requerimientos Externos → Módulo Cliente)

Administración depende de estos 4 servicios expuestos por este módulo:

| # | Servicio | Debe incluir | No debe filtrar por |
|---|---|---|---|
| 1 | Obtener Clientes | Activos e inactivos | — |
| 2 | Total Histórico de Transacciones | Depósitos, pagos, retiros, transferencias y cualquier otro tipo | Usuario, fecha, tipo |
| 3 | Total de Transacciones del Día | Mismo alcance que el anterior | Usuario, tipo (solo filtra por fecha actual) |
| 4 | Registro de Transacción Inicial | Registrar automáticamente la transacción inicial de un cliente cuya cuenta se creó con saldo inicial (funcional, pág. 26) | — |

> El servicio "Obtener Clientes" (#1) en rigor es un contrato de **Identity** según el
> documento de Entidades y Contratos Base (sección "Contratos consumidos desde Identity"); el
> documento de Requerimientos Externos lo agrupa bajo "Módulo Cliente" porque Administración
> lo necesita en ese contexto. **Confirmar con el equipo quién implementa físicamente "Obtener
> Clientes"** antes de codificar — puede ser un simple passthrough a Identity desde este
> módulo, o competencia directa de Identity.

### `RegisterInitialTransactionAsync` — el más relevante para ti

Lo consume la creación de cuenta principal ([Administrador] `SavingsAccountService.AddAsync`)
y la asignación de cuenta secundaria con balance inicial > 0. Este método:

- Recibe: `SavingsAccountId`, `Amount`.
- Registra una `Transaction` **CRÉDITO**, `OperationType = AperturaCuenta`,
  `Channel = Cliente` (o el canal que corresponda según quién dispara la apertura),
  `Origin` = literal apropiado (p. ej. `DEPÓSITO` o el origen que el equipo defina para
  apertura de cuenta — **no especificado literalmente** en el documento funcional para este
  caso puntual; usar el mismo criterio que balance inicial de cuenta).
- **No** abre su propia transacción de base de datos: se invoca **dentro** de la unidad de
  trabajo que ya abrió `SavingsAccountService.AddAsync`, para que cuenta + transacción se
  confirmen juntas (ver Contratos Base, inventario de operaciones atómicas).

## Logging (Serilog) — qué registrar y qué nunca registrar

Cada método de `TransactionService`/`PaymentService` debe generar:

- Un log **informativo** al completar la operación: fecha/hora, `ClientId` autenticado, tipo
  de operación (`OperationType`), monto efectivo, resultado (APROBADA/RECHAZADA),
  identificador de correlación.
- Un log de **error** ante cualquier excepción no controlada, antes de traducirla a
  Problem Details.

**Nunca** se registra: número completo de tarjeta, CVC o su hash, contraseñas, tokens. Para
identificar una tarjeta en el log de un pago se usa únicamente `LastFourDigits`.
