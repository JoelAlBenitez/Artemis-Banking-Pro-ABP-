# Directrices Oficiales de Construcción y Codificación - Artemis Banking Pro (ABP)

Este documento establece los estándares obligatorios de desarrollo y codificación en el backend para la solución ABP. Todo agente de desarrollo e integrante del equipo debe cumplir estas directrices al implementar o modificar servicios y validadores.

---

## 1. Comprobación Obligatoria de `SaveChangesAsync()`
Toda operación de persistencia o modificación en la base de datos a través de EF Core debe validar el resultado del guardado:
* **Regla**: Capturar el entero de retorno de `SaveChangesAsync()`.
* **Acción**: Si el valor devuelto es `<= 0` (cero o menor), se debe:
  1. Registrar un `LogWarning` describiendo la operación fallida y el identificador de los datos involucrados.
  2. Retornar un resultado de validación fallido (`ValidationResult.Failure`) utilizando el error general de la solución: `GeneralError.UnexpectedError`.
* **Ejemplo**:
  ```csharp
  var affectedRows = await _repository.SaveChangesAsync();
  if (affectedRows <= 0)
  {
      _logger.LogWarning("Fallo al persistir la transacción para el cliente {ClientId}", clientId);
      return ValidationResult.Failure(GeneralError.UnexpectedError);
  }
  ```

---

## 2. Modularización y Métodos Auxiliares Privados (Helper Methods)
Para mantener la legibilidad y facilitar el mantenimiento del código, ningún método de servicio público debe concentrar lógica excesiva:
* **Regla**: Los métodos públicos deben comportarse estrictamente como orquestadores de alto nivel. Su longitud máxima ideal debe ser de **45 a 50 líneas**.
* **Acción**: Separar la lógica en métodos privados especializados:
  * Creación y asignación de propiedades de entidades.
  * Lógicas algorítmicas secuenciales (ej. cobros cronológicos de cuotas).
  * Construcción y envío de notificaciones de correo.

---

## 3. Estructura y Trazabilidad de Logs (`ILogger`)
El uso de `ILogger` debe seguir un esquema estructurado en todas las capas de servicios y validadores:

### A. Al inicio de cada método (Nivel Informativo)
* **Log**: `LogInformation` detallando los parámetros clave de la solicitud.
* **Ejemplo**:
  ```csharp
  _logger.LogInformation("Iniciando validación de transferencia express para el cliente {ClientId} por monto RD${Amount}", clientId, dto.Amount);
  ```

### B. En fallos de reglas de negocio o datos (Nivel de Advertencia)
* **Log**: `LogWarning` con los detalles del motivo del rechazo o validación inválida.
* **Ejemplo**:
  ```csharp
  _logger.LogWarning("El cliente {ClientId} intentó realizar una transacción con fondos insuficientes en la cuenta {AccountNumber}", clientId, dto.SourceAccountNumber);
  ```

### C. En excepciones no controladas o fallas de base de datos (Nivel de Error)
* **Log**: `LogError(ex, "Mensaje explicativo")` envolviendo las operaciones críticas en bloques `try-catch`.
* **Ejemplo**:
  ```csharp
  catch (Exception ex)
  {
      _logger.LogError(ex, "Error crítico al procesar el pago de tarjeta del cliente {ClientId}", clientId);
      return ValidationResult.Failure(GeneralError.UnexpectedError);
  }
  ```

---

## 4. Gestión de Notificaciones de Correo
Toda transacción que requiera el envío de notificaciones de correo mediante `IEmailServices.SendNotification()` debe ajustarse a las siguientes reglas:
* **No bloqueante para el flujo principal**: El envío de correo debe realizarse **fuera** de la transacción física de base de datos de EF Core (después de llamar exitosamente a `SaveChangesAsync()`).
* **No reversibilidad**: Un fallo al enviar un correo electrónico **no debe revertir** la transacción financiera en la base de datos.
* **Captura de resultados y advertencias**:
  1. El método del correo debe ser esperado (`await`) y su resultado `bool` capturado.
  2. Si el retorno es `false` o se genera una excepción en el envío:
     - Registrar el error con `LogError`.
     - Poblar el campo `WarningMessage` del DTO de resultado (`TransactionResultDto`) con el mensaje oficial: *"La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo."*
  3. En la capa de Presentación, el controlador debe capturar este mensaje y propagarlo al usuario final mediante `TempData["SuccessMessage"]`.

---

## 5. Encapsulación de Parámetros en DTOs
Por motivos de seguridad, mantenibilidad y consistencia del diseño de la arquitectura de la aplicación:
* **Regla**: Los métodos expuestos por las interfaces de servicios (`Application Contracts`) no deben aceptar múltiples parámetros primitivos sueltos si estos representan una entidad o transacción.
* **Acción**: Agrupar y encapsular todos los parámetros de entrada de los servicios dentro de un Objeto de Transferencia de Datos (**DTO**).
* **Ejemplo**:
  * *Incorrecto*: `Task<ValidationResult> RegisterInitialTransactionAsync(int savingsAccountId, decimal amount, string performedByUserId)`
  * *Correcto*: `Task<ValidationResult> RegisterInitialTransactionAsync(InitialTransactionDto dto)`

---

## 6. Uso de Enums del Dominio en DTOs y Mapeos
Para asegurar la integridad de tipos en la capa de Aplicación y simplificar los mapeos:
* **Regla**: Las propiedades de los DTOs que correspondan a estados, tipos o categorizaciones representadas en el dominio deben declararse utilizando el tipo `enum` correspondiente del Dominio, y no como cadenas de texto (`string`).
* **Mapeo a la Presentación**: La conversión a cadena de texto para elementos puramente visuales debe realizarse únicamente al mapear el DTO hacia el ViewModel correspondiente de la capa de Presentación.
* **Ejemplo**:
  * *Incorrecto*: `public string Status { get; set; }` en `TransactionResultDto`.
  * *Correcto*: `public TransactionStatus Status { get; set; }` en `TransactionResultDto`, mapeándose a `string` posteriormente en el ViewModel.

---

## 7. Uso del Modificador `required` en DTOs y ViewModels
Para evitar contradicciones entre la validación de enlace de datos y las declaraciones de tipos de C#:
* **Regla**: Las propiedades obligatorias de cadenas de texto (`string`) en los DTOs y ViewModels no deben inicializarse con valores por defecto dummy (como `= string.Empty;` o `""`) si están decoradas con el atributo `[Required]`.
* **Acción**: Declarar las propiedades utilizando el modificador `required` de C# en su lugar. Esto asegura la validación en tiempo de compilación y evita inicializaciones redundantes.
* **Ejemplo**:
  * *Incorrecto*:
    ```csharp
    [Required(ErrorMessage = "Campo requerido.")]
    public string Name { get; set; } = string.Empty;
    ```
  * *Correcto*:
    ```csharp
    [Required(ErrorMessage = "Campo requerido.")]
    public required string Name { get; set; }
    ```

---

## 8. Implementación de Baja Lógica (Soft Delete)
Para entidades que requieran desactivación en lugar de borrado físico (por ejemplo, `Beneficiary`):
* **Propiedades de Dominio**: Deben incluir `IsActive` (bool) y `DeactivatedAt` (DateTimeOffset?).
* **Filtro Global en Persistencia**: Debe añadirse en la configuración de la entidad en EF Core (`builder.HasQueryFilter(e => e.IsActive);`) para que las consultas omitan registros inactivos por defecto.
* **Flujo en el Servicio**:
  1. Validar que la entidad pertenezca al cliente autenticado.
  2. Establecer `IsActive = false` y `DeactivatedAt = DateTimeOffset.UtcNow`.
  3. Establecer las propiedades de auditoría (`LastModifiedByIdUser`, `ModifiedAt`).
  4. Guardar mediante `SaveChangesAsync()` y verificar que el retorno sea mayor que cero.





