# Beneficiarios — Reglas de negocio (resumen)

Reglas adicionales del módulo (documento funcional, pág. 81):

- Solo el rol **Cliente** accede a la funcionalidad de Beneficiarios.
- Cada cliente solo puede visualizar y administrar **sus propios** beneficiarios.
- Un beneficiario debe corresponder a una **cuenta de ahorro activa existente**.
- No se pueden registrar cuentas **canceladas** como beneficiarios.
- No se pueden registrar **cuentas propias** como beneficiarios.
- No se puede registrar **dos veces** la misma cuenta como beneficiario del mismo cliente.
- El nombre y apellido del beneficiario se obtienen **automáticamente** desde el propietario
  de la cuenta (Identity), nunca se capturan por formulario.
- Eliminar un beneficiario solo elimina la **relación** con el cliente autenticado.
- Eliminar un beneficiario **no** elimina la cuenta de ahorro ni modifica transacciones
  históricas.

## Checklist de la rúbrica (Funcionalidades del cliente — Beneficiarios)

- [ ] Gestión de beneficiarios con validación de cuenta activa y no propia.
- [ ] Eliminación de beneficiarios sin afectar historial ni cuentas.
