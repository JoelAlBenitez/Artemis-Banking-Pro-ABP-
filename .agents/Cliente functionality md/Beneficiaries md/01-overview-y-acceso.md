# Beneficiarios — Overview y acceso

## Propósito

Al ingresar a **Beneficiarios** desde el menú del cliente, el sistema envía al módulo de
administración de beneficiarios propio del cliente autenticado. Permite registrar cuentas de
ahorro de otros clientes como beneficiarios frecuentes, para no ingresar manualmente el
número de cuenta en cada transacción.

## Acceso por rol

- Solo usuarios con rol **Cliente** pueden acceder.
- Si un usuario con rol **Administrador, Cajero o Comercio** intenta acceder directamente por
  URL, el sistema lo redirige a **Acceso denegado**.

## Flujos del módulo

1. **Listado** de beneficiarios del cliente autenticado → [03](./03-listado.md)
2. **Agregar beneficiario** (modal/pantalla + formulario) → [04](./04-agregar-beneficiario.md)
3. **Eliminar beneficiario** (confirmación + baja lógica) → [05](./05-eliminar-beneficiario.md)

## Dependencias externas

- **Datos del propietario de la cuenta beneficiaria** (nombre, apellido) → Identity.
- **Existencia y estado de la cuenta** a registrar → `SavingsAccountRepository` (módulo
  Administrador / dominio compartido).
