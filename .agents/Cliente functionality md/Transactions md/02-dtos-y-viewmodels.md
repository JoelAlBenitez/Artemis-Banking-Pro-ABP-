# Transacciones — DTOs y ViewModels

## `ExpressTransactionDto` (escritura)

| Campo | Tipo | Nota |
|---|---|---|
| `SourceAccountNumber` | string(9) | Cuenta origen (propia, activa) |
| `DestinationAccountNumber` | string(9) | Cuenta destino (cualquier cuenta activa del sistema) |
| `Amount` | decimal | > 0 |

## `PayCreditCardDto` (escritura)

| Campo | Tipo | Nota |
|---|---|---|
| `SourceAccountNumber` | string(9) | Cuenta origen propia y activa |
| `CreditCardId` | int | Tarjeta propia y activa, con deuda pendiente |
| `Amount` | decimal | Monto **solicitado**; el efectivo puede ser menor (anti-sobrepago) |

## `PayLoanDto` (escritura)

| Campo | Tipo | Nota |
|---|---|---|
| `SourceAccountNumber` | string(9) | Cuenta origen propia y activa |
| `LoanId` | int | Préstamo propio y activo con cuotas pendientes |
| `Amount` | decimal | Monto **solicitado**; el efectivo puede ser menor |

## `BeneficiaryTransactionDto` (escritura)

| Campo | Tipo | Nota |
|---|---|---|
| `SourceAccountNumber` | string(9) | Cuenta origen propia y activa |
| `BeneficiaryId` | int | Beneficiario previamente registrado por el cliente autenticado |
| `Amount` | decimal | > 0 |

## `TransactionResultDto` (lectura — resultado de cualquiera de las 4 operaciones)

| Campo | Tipo | Nota |
|---|---|---|
| `EffectiveAmount` | decimal | Monto realmente aplicado (relevante en pagos) |
| `TransactionType` | string | DÉBITO / CRÉDITO |
| `Status` | string | APROBADA / RECHAZADA |
| `CreatedAt` | DateTime | Fecha y hora exacta |

## ViewModels (inventario, ver Contratos Base 7.3)

`ExpressTransactionViewModel`, `PayCardViewModel`, `PayLoanViewModel`,
`BeneficiaryTransactionViewModel` — todos con `SourceAccountId` (Select, solo cuentas activas
propias), monto (`[Range(0.01, ...)]`) y el selector propio de cada pantalla (cuenta destino /
tarjeta / préstamo / beneficiario).

## `ConfirmationViewModel` (reutilizado)

Las 4 pantallas muestran una confirmación previa con el titular del destino, el identificador
del producto y el monto — usando el `ConfirmationViewModel` genérico de la base compartida.

> **UI / vistas: No desarrollado por el agente de IA.**
