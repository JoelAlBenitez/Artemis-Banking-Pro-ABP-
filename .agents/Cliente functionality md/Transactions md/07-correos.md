# Transacciones — Correos de notificación

Todos los correos se envían **fuera de la transacción** (`IEmailService`). Un fallo de correo
**no revierte** la operación: se registra el error y se informa al cliente.

Mensaje genérico si falla el envío: **«La transacción fue realizada correctamente, pero no
fue posible enviar una o más notificaciones por correo.»**

## Transacción Express (2 correos)

**Al emisor** — asunto: *"Transacción realizada a la cuenta [XXXX]"* (últimos 4 del destino).
Incluye: monto, fecha, hora, últimos 4 de la cuenta destino.

**Al receptor** — asunto: *"Transacción enviada desde la cuenta [XXXX]"* (últimos 4 del
origen). Incluye: monto, fecha, hora, últimos 4 de la cuenta origen.

## Pago a tarjeta de crédito (1 correo)

Asunto: *"Pago realizado a la tarjeta [XXXX]"*. Incluye: monto pagado, últimos 4 de la cuenta
origen, últimos 4 de la tarjeta pagada, fecha, hora.

## Pago a préstamo (1 correo)

Asunto: *"Pago realizado al préstamo [XXXXXXXXX]"* (9 dígitos). Incluye: monto pagado, número
de préstamo, últimos 4 de la cuenta origen, fecha, hora.

## Transacción a beneficiarios (2 correos)

Igual estructura que Transacción Express: uno al emisor, uno al receptor.

## Datos prohibidos en el cuerpo del correo

Número completo de tarjeta, CVC, contraseñas, tokens — nunca se incluyen (regla transversal
del sistema).
