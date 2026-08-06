# Transacciones — Overview y acceso

## Propósito

El módulo de Transacciones permite que el cliente autenticado realice operaciones
financieras desde sus cuentas de ahorro activas. Cuatro pantallas principales:

1. Transacción Express
2. Pago a tarjeta de crédito
3. Pago a préstamo
4. Transacción a beneficiarios

## Acceso por rol

- Solo usuarios con rol **Cliente** pueden acceder.
- Si un usuario con rol **Administrador, Cajero o Comercio** intenta acceder directamente a
  cualquiera de estas pantallas por URL, el sistema lo redirige a **Acceso denegado**.

## Regla transversal

Todas las operaciones deben validar los datos ingresados **antes** de afectar balances,
deudas o historiales. Cuando una transacción es aprobada, el sistema registra los
movimientos correspondientes y actualiza los balances involucrados.

## Dependencias externas

- **Cuentas, tarjetas y préstamos activos del cliente autenticado** → repositorios del
  dominio compartido (`SavingsAccountRepository`, `CreditCardRepository`, `LoanRepository`).
- **Datos del titular de la cuenta destino / beneficiario** → Identity.
- **Distribución del pago sobre cuotas** (pago a préstamo) → `IPaymentAllocator`.
- **Usuario autenticado** → `ICurrentUserService`.
