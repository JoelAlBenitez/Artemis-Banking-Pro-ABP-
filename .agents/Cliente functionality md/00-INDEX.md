# Índice de documentación técnica — Módulo Cliente (ABP)

Documentación de referencia rápida para desarrollar el **módulo de Funcionalidades del
Cliente** de Artemis Banking Pro. Cada archivo es pequeño y representa aproximadamente una
tarea de **menos de 10 cambios antes de un commit**.

> Toda la información proviene únicamente de los documentos oficiales del proyecto
> (documento funcional, Entidades de Negocio y Contratos Base, rúbrica de evaluación y
> Requerimientos Externos para Administración). Lo que no aparece explícitamente se marca
> como **No especificado**.

## Responsable

**Sebastian** — módulo **Funcionalidades del Cliente**:

1. Beneficiarios
2. Transacciones (Express, Pago a tarjeta, Pago a préstamo, Transacción a beneficiarios)
3. Avances de efectivo
4. Transferencias entre cuentas propias

## Cómo está organizada

| Carpeta / archivo | Contenido | ¿Lo desarrollo yo? |
|---|---|---|
| [`01-restricciones-tecnicas-transversales.md`](./01-restricciones-tecnicas-transversales.md) | Onion Architecture, transacciones EF Core, anti-sobrepago, Serilog, escalabilidad, alcance de pruebas. **Leer antes de codificar cualquier servicio.** | — |
| [`Client Home md/`](./Client%20Home%20md/README.md) | Home del cliente: menú y listado de productos financieros activos. | Sí |
| [`Beneficiaries md/`](./Beneficiaries%20md/README.md) | Gestión de beneficiarios (agregar, listar, eliminar). | Sí |
| [`Transactions md/`](./Transactions%20md/README.md) | Transacción Express, pago a tarjeta, pago a préstamo, transacción a beneficiarios. | Sí |
| [`CashAdvance md/`](./CashAdvance%20md/README.md) | Avance de efectivo desde tarjeta hacia cuenta propia. | Sí |
| [`AccountTransfer md/`](./AccountTransfer%20md/README.md) | Transferencia entre cuentas de ahorro propias. | Sí |

## Alcance del módulo Cliente (documento funcional, páginas 69–105)

| Funcionalidad | Páginas |
|---|---|
| Funcionalidades cliente (Home) | 69–77 |
| Beneficiarios | 77–81 |
| Transacciones | 81–94 |
| Avances de efectivo | 94–100 |
| Transferencia entre cuentas | 100–105 |

## Entidades que ya existen (referencia — no se redefinen)

Definidas en *Entidades de Negocio y Contratos Base - ABP*. Este módulo las **consume y
escribe sobre ellas**, no las redefine:

- **`Transaction`** — entidad central e inmutable. Toda operación de este módulo crea uno o
  dos registros de `Transaction` (DÉBITO/CRÉDITO).
- **`Beneficiary`** — relación cliente → cuenta de otro cliente. Único caso de baja lógica
  pura (`IsActive = false`) de todo el dominio.
- **`SavingsAccount`** — se lee y se actualiza el `Balance`; no se crea ni se cancela desde
  aquí (eso es de Administrador).
- **`CreditCard`** — se lee y se actualiza `OwedAmount`; no se asigna ni se cancela desde aquí.
- **`Loan`** — se lee y se actualiza `PendingAmount` / estado de `LoanInstallment`; no se
  asigna ni se edita la tasa desde aquí.
- **`CashAdvance`**, **`CardPayment`**, **`LoanPayment`** — entidades de trazabilidad que
  **este módulo origina** (documentadas en detalle en sus respectivas carpetas).

## Servicios que no heredan del genérico (documento de Contratos Base, sección 9.3)

> *"Las operaciones puramente transaccionales — `TransactionService`, `PaymentService`,
> `CashAdvanceService` — no son CRUD sobre una entidad: son casos de uso que coordinan varias
> entidades. Se implementan como servicios de aplicación independientes con métodos propios
> de intención, y siempre dentro de una unidad de trabajo explícita."*

`BeneficiaryService` **sí** hereda del servicio genérico (es CRUD simple con overrides).

## Contrato que este módulo debe exponer (Requerimientos Externos)

El módulo Cliente debe exponer a Administración:

| # | Servicio | Descripción |
|---|---|---|
| 1 | Obtener Clientes | Todos los clientes, activos e inactivos |
| 2 | Total Histórico de Transacciones | Todas las transacciones desde el inicio; sin filtros |
| 3 | Total de Transacciones del Día | Solo filtro por fecha actual |
| 4 | Registro de Transacción Inicial | Registra la transacción inicial de una cuenta creada con saldo (pág. 26) |

Ver detalle en [`Transactions md/08-servicio-y-repositorio.md`](./Transactions%20md/08-servicio-y-repositorio.md).

## Convenciones transversales (heredadas de la base compartida)

- **No borrado físico:** toda baja es cambio de estado (`Beneficiary.IsActive = false`). No existe `DeleteAsync`.
- **Atomicidad:** cada operación compuesta corre dentro de `IUnitOfWork.ExecuteInTransactionAsync`.
- **AutoMapper obligatorio** como única frontera de conversión entre capas.
- **Correos siempre fuera de la transacción**; un fallo de envío no revierte la operación.
- **Intentos rechazados se persisten** como `Transaction` RECHAZADA (o `CardConsumption`
  RECHAZADO en el caso del avance), sin afectar balances ni deudas.
- **Validaciones de negocio antes de abrir la transacción.**
- Datos sensibles (número completo de tarjeta, CVC, hash) nunca se exponen ni se registran.

## Exclusiones — No desarrollado por el agente de IA

- Pruebas unitarias y de integración.
- Interfaz de usuario (UI / vistas Razor).
- Azure Function de cuotas atrasadas (no aplica a este módulo, pero se referencia si un
  préstamo pagado por el cliente cambia su estado de mora).
