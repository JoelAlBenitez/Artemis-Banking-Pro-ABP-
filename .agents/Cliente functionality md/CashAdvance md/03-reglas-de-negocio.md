# Avance de efectivo — Reglas de negocio (resumen)

Reglas adicionales del módulo (documento funcional, pág. 99–100):

- Solo el rol **Cliente** accede a la funcionalidad de Avance de efectivo.
- El cliente solo puede utilizar **tarjetas de crédito activas** que le pertenezcan.
- El cliente solo puede seleccionar **cuentas de ahorro activas** que le pertenezcan.
- El monto del avance debe ser **mayor que cero**.
- La operación valida el **crédito disponible** antes de afectar balances o deudas.
- El crédito disponible considera la **deuda actual** de la tarjeta.
- El total cargado a la tarjeta incluye el monto del avance **más el interés del 6.25 %**
  (`DomainConstants.CashAdvanceInterestRate = 0.0625m`).
- El total cargado **no puede superar** el crédito disponible.
- El monto depositado en la cuenta de ahorro es **únicamente** el monto del avance solicitado
  (el interés no se deposita, solo se carga a la tarjeta).
- La cuenta de ahorro destino registra la operación como **CRÉDITO**.
- La tarjeta de crédito registra la operación como consumo de tipo **AVANCE**.
- Los avances **rechazados** no modifican el balance de la cuenta ni la deuda de la tarjeta.
- Los avances **aprobados** actualizan el balance de la cuenta, la deuda de la tarjeta y el
  crédito disponible.
- La operación **no** se revierte si falla el envío del correo electrónico.
- Al finalizar, el sistema redirige al cliente al **Home del cliente**.

## Checklist de la rúbrica (Funcionalidades del cliente — Avances de efectivo)

- [ ] Avance de efectivo con interés del 6.25% y validación de crédito disponible.
