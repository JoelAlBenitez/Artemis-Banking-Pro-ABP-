

Evaluación Proyecto Final: Artemis Banking Pro (ABP)
Requerimientos Funcionales, Técnicos y Pruebas
Criterio de evaluaciónValor total de puntosPuntos obtenido
Funcionalidades generales y seguridad WebApp
Login web con validación de credenciales, usuario activo y rol permitido
## 20
## 0
Redirección correcta al Home según rol autenticado
## 20
## 0
Activación de cuenta mediante enlace/token de un solo uso
## 20
## 0
Restablecimiento de contraseña con token vigente y confirmación de contraseña
## 20
## 0
Menú principal con opciones correspondientes según rol
## 20
## 0
Navegación correcta entre módulos de la WebApp
## 20
## 0
Uso consistente del layout general de la aplicación
## 20
## 0
Mensajes de validación y confirmación claros para el usuario
## 20
## 0
Restricción de acceso directo por URL según rol
## 20
## 0
Creación por seeding de roles y usuarios por defecto activos
## 20
## 0
Home del administrador
Redirección correcta al Home del administrador luego del login
## 20
## 0
Menú del administrador con todos los módulos requeridos
## 20
## 0
Indicadores generales calculados correctamente
## 20
## 0
Cálculo correcto de transacciones históricas y transacciones del día
## 20
## 0
Cálculo correcto de pagos históricos y pagos del día
## 20
## 0
Cálculo correcto de clientes activos e inactivos
## 20
## 0
Cálculo correcto de productos financieros activos
## 20
## 0
Cálculo correcto de deuda promedio por cliente
## 20
## 0
Gestión de usuarios WebApp
Listado paginado de usuarios y filtro por rol
## 20
## 0
Creación de usuarios Administrador, Cajero y Cliente
## 20
## 0
Validación de usuario, correo y cédula únicos
## 20
## 0
Validación de contraseña y confirmación de contraseña
## 20
## 0
Creación de cliente con cuenta de ahorro principal automática
## 20
## 0

Registro de monto inicial como crédito cuando aplique
## 20
## 0
Envío de correo de activación al crear usuario
## 20
## 0
Edición de usuario sin permitir modificar el rol
## 20
## 0
Manejo de monto adicional para clientes y registro de crédito
## 20
## 0
Activación e inactivación de usuarios con bloqueo de auto-modificación
## 20
## 0
Gestión de préstamos WebApp
Listado de préstamos con paginación, filtros por estado y búsqueda por cédula
## 20
## 0
Asignación de préstamo solo a cliente activo sin préstamo activo
## 20
## 0
Validación de cliente de alto riesgo según deuda promedio
## 20
## 0
Cálculo correcto de cuota bajo sistema francés
## 20
## 0
Generación correcta de tabla de amortización
## 20
## 0
Generación de número de préstamo único de 9 dígitos como texto
## 20
## 0
Desembolso del préstamo a la cuenta principal del cliente
## 20
## 0
Registro del desembolso como transacción de tipo crédito
## 20
## 0
Detalle de préstamo con tabla de amortización y estado de cuotas
## 20
## 0
Edición de tasa recalculando solo cuotas futuras pendientes
## 20
## 0
Gestión de tarjetas de crédito WebApp
Listado de tarjetas con paginación, filtros y búsqueda por cédula
## 20
## 0
Asignación de tarjeta a cliente activo
## 20
## 0
Generación de número de tarjeta único de 16 dígitos
## 20
## 0
Generación de fecha de expiración y CVC
## 20
## 0
Almacenamiento del CVC como hash y no como texto plano
## 20
## 0
Visualización de tarjeta enmascarada y últimos cuatro dígitos
## 20
## 0
Detalle de tarjeta con consumos aprobados y rechazados
## 20
## 0
Edición de límite validando que no sea menor a la deuda actual
## 20
## 0
Cancelación de tarjeta únicamente si no tiene deuda pendiente
## 20
## 0
Notificaciones por correo al asignar o modificar tarjeta
## 20
## 0
Gestión de cuentas de ahorro WebApp
Listado de cuentas con paginación, filtros por estado, tipo y cédula
## 20
## 0
Asignación de cuenta secundaria solo a cliente activo
## 20
## 0

Validación de existencia de cuenta principal activa antes de crear secundaria
## 20
## 0
Generación de número de cuenta único de 9 dígitos como texto
## 20
## 0
Registro del balance inicial como crédito cuando aplique
## 20
## 0
Detalle de cuenta con historial de transacciones
## 20
## 0
Cancelación exclusiva de cuentas secundarias activas
## 20
## 0
Transferencia automática del balance a cuenta principal al cancelar secundaria
## 20
## 0
Registro cruzado de débito y crédito al cancelar cuenta con balance
## 20
## 0
Bloqueo de operaciones sobre cuentas canceladas
## 20
## 0
Funcionalidades del cliente
Home del cliente con listado de productos financieros activos
## 20
## 0
Visualización de detalles de cuentas, préstamos y tarjetas
## 20
## 0
Gestión de beneficiarios con validación de cuenta activa y no propia
## 20
## 0
Eliminación de beneficiarios sin afectar historial ni cuentas
## 20
## 0
Transacción Express con validación de cuenta destino y fondos
## 20
## 0
Pago de tarjeta de crédito desde cuenta propia sin permitir sobrepago
## 20
## 0
Pago de préstamo aplicando cuotas en orden de antigüedad
## 20
## 0
Transacción a beneficiarios con registro de débito y crédito
## 20
## 0
Avance de efectivo con interés del 6.25% y validación de crédito disponible
## 20
## 0
Transferencia entre cuentas propias con validación de origen y destino distintos
## 20
## 0
Correos de notificación y registros de historial en operaciones del cliente
## 20
## 0
Funcionalidades del cajero
Home del cajero con indicadores del día calculados correctamente
## 20
## 0
Depósito a cuenta de ahorro con validaciones y registro como crédito
## 20
## 0
Retiro desde cuenta de ahorro con validación de fondos y registro como débito
## 20
## 0
Pago a tarjeta de crédito desde cuenta de ahorro con validación de deuda
## 20
## 0
Pago a préstamo desde cuenta de ahorro aplicando cuotas en orden
## 20
## 0
Transacciones a cuentas de terceros con registro cruzado de débito y crédito
## 20
## 0
Registro de intentos rechazados cuando aplique sin afectar balances
## 20
## 0
Asociación de operaciones al cajero autenticado
## 20
## 0
Confirmaciones previas a operaciones financieras del cajero
## 20
## 0

Correos de notificación al cliente emisor y receptor cuando corresponda
## 20
## 0
Seguridad general de la Web API
Autenticación JWT configurada correctamente
## 20
## 0
Autorización por roles en endpoints protegidos
## 20
## 0
Respuesta 401 para token ausente, inválido o expirado
## 20
## 0
Respuesta 403 para usuario autenticado sin permisos
## 20
## 0
Separación correcta de roles Administrador y Comercio en la API
## 20
## 0
Endpoints públicos de Account disponibles sin JWT cuando corresponda
## 20
## 0
Usuarios API creados inactivos hasta confirmación o restablecimiento
## 20
## 0
JWT con identificador de usuario, nombre de usuario, rol y expiración
## 20
## 0
Módulo API: Account Controller
POST /account/login con validación de credenciales y retorno de JWT
## 20
## 0
POST /account/confirm con validación de token y activación de usuario
## 20
## 0
POST /account/get-reset-token con inactivación temporal y envío de token
## 20
## 0
POST /account/reset-password con validación de token y cambio de contraseña
## 20
## 0
Manejo correcto de respuestas 200, 204, 400, 401 y 403 según escenario
## 20
## 0
Tokens de confirmación y restablecimiento de un solo uso
## 20
## 0
Módulo API: Gestión de usuarios
GET /api/users con listado paginado excluyendo usuarios Comercio
## 20
## 0
GET /api/users/commerce con listado paginado de usuarios Comercio
## 20
## 0
POST /api/users para crear Administrador, Cajero o Cliente
## 20
## 0
POST /api/users/commerce/{commerceId} para crear usuario Comercio
## 20
## 0
PUT /api/users/{id} para actualizar usuario sin modificar rol
## 20
## 0
PATCH /api/users/{id}/status para activar o inactivar usuarios
## 20
## 0
GET /api/users/{id} para obtener detalle de usuario
## 20
## 0
Validación de unicidad de usuario, correo y cédula
## 20
## 0
Creación automática de cuenta principal para Cliente y Comercio
## 20
## 0
Validación de un solo usuario asociado por comercio
## 20
## 0
Módulo API: Gestión de préstamos
GET /api/loan con paginación, filtros y búsqueda por cédula
## 20
## 0

POST /api/loan con asignación de préstamo y tabla de amortización
## 20
## 0
Validación de cliente sin préstamo activo
## 20
## 0
Validación de alto riesgo con respuesta 409 Conflict cuando aplique
## 20
## 0
Acreditación del préstamo a cuenta principal como crédito
## 20
## 0
GET /api/loan/{id} con detalle y tabla de amortización
## 20
## 0
PATCH /api/loan/{id}/rate recalculando solo cuotas futuras pendientes
## 20
## 0
Módulo API: Gestión de tarjetas de crédito
GET /api/credit-card con paginación, filtros y búsqueda por cédula
## 20
## 0
POST /api/credit-card con asignación de tarjeta a cliente activo
## 20
## 0
Generación segura de número, expiración y CVC hasheado
## 20
## 0
GET /api/credit-card/{id} con detalle y consumos
## 20
## 0
PATCH /api/credit-card/{id}/limit validando deuda actual
## 20
## 0
PATCH /api/credit-card/{id}/cancel validando deuda cero
## 20
## 0
Módulo API: Gestión de cuentas de ahorro
GET /api/savings-account con paginación y filtros
## 20
## 0
POST /api/savings-account para crear cuenta secundaria
## 20
## 0
Validación de cliente activo con cuenta principal activa
## 20
## 0
GET /api/savings-account/{accountNumber}/transactions con historial paginado
## 20
## 0
PATCH /api/savings-account/{accountNumber}/cancel para cancelar secundaria
## 20
## 0
Transferencia de balance a principal y registro de movimientos al cancelar
## 20
## 0
Módulo API: Gestión de comercios
GET /api/commerce con listado paginado de comercios
## 20
## 0
GET /api/commerce/{id} con detalle del comercio
## 20
## 0
POST /api/commerce con validación de RNC y correo únicos
## 20
## 0
PUT /api/commerce/{id} para actualizar datos sin modificar estado
## 20
## 0
PATCH /api/commerce/{id}/status para activar o desactivar comercio
## 20
## 0
Inactivación de usuarios asociados al desactivar comercio
## 20
## 0
Reactivación de comercio sin activar automáticamente sus usuarios
## 20
## 0
Módulo API: Procesador de pago Hermes Pay
Control de acceso para roles Administrador y Comercio
## 20
## 0

Uso del commerceId desde JWT cuando el usuario autenticado es Comercio
## 20
## 0
Uso del commerceId de la URL cuando el usuario autenticado es Administrador
## 20
## 0
GET /pay/get-transactions/{commerceId} con transacciones paginadas
## 20
## 0
POST /pay/process-payment/{commerceId} con validación de tarjeta y comercio
## 20
## 0
Validación de número de tarjeta, expiración y CVC
## 20
## 0
Validación de crédito disponible antes de aprobar consumo
## 20
## 0
Registro de consumo aprobado y aumento de deuda de tarjeta
## 20
## 0
Acreditación del pago en cuenta principal del comercio
## 20
## 0
Registro de consumo rechazado sin modificar balances ni deudas
## 20
## 0
Correos al cliente y al comercio luego de pago aprobado
## 20
## 0
Reglas financieras y trazabilidad
Uso correcto de tipo DÉBITO para salidas de dinero
## 20
## 0
Uso correcto de tipo CRÉDITO para entradas de dinero
## 20
## 0
Registro cruzado en transferencias entre cuentas
## 20
## 0
No permitir sobrepagos a tarjetas ni préstamos
## 20
## 0
Actualización correcta de deuda, balance y crédito disponible
## 20
## 0
Ejecución transaccional de operaciones que afectan múltiples entidades
## 20
## 0
Conservación del historial aunque productos o usuarios sean inactivados/cancelados
## 20
## 0
Uso de decimal para montos monetarios y precisión hasta centavos
## 20
## 0
Reglas técnicas y arquitectura
Implementación en ASP.NET Core MVC y Web API con .NET 9
## 20
## 0
Uso correcto de Entity Framework Core Code First
## 20
## 0
Creación correcta de entidades, relaciones, configuraciones y migraciones
## 20
## 0
Implementación correcta de Onion Architecture
## 20
## 0
Separación adecuada de capas Domain, Application, Infrastructure, Persistence, WebApp y WebAPI
## 20
## 0
Uso correcto de ViewModels y validaciones en la WebApp
## 20
## 0
Uso correcto de DTOs para transferencia de información en la API
## 20
## 0
Uso correcto de AutoMapper entre entidades, ViewModels, DTOs, Commands y Queries
## 20
## 0
Uso de repositorios genéricos y repositorios específicos cuando aplique
## 20
## 0
Uso de servicios genéricos y servicios de negocio por módulo
## 20
## 0

Controladores sin lógica de negocio compleja
## 20
## 0
Interfaz visual clara usando Bootstrap u otro framework CSS
## 20
## 0
CQRS, Mediator, Behaviors y validaciones
Implementación de CQRS en endpoints de Account
## 20
## 0
Implementación de CQRS en endpoints de usuarios y comercios
## 20
## 0
Implementación de CQRS en endpoints de préstamos
## 20
## 0
Implementación de CQRS en endpoints de tarjetas de crédito
## 20
## 0
Implementación de CQRS en endpoints de cuentas de ahorro
## 20
## 0
Implementación de CQRS en endpoints de Hermes Pay
## 20
## 0
Uso correcto de Mediator para Commands y Queries
## 20
## 0
Validaciones de Commands y Queries mediante FluentValidation
## 20
## 0
Uso de Behaviors para validaciones transversales
## 20
## 0
Separación entre validaciones estructurales y reglas de negocio con acceso a datos
## 20
## 0
Validación de servicios por módulo
Servicios de seguridad, login, activación y restablecimiento validados correctamente
## 20
## 0
Servicios de usuarios y roles validados correctamente
## 20
## 0
Servicios de cuentas de ahorro y transacciones validados correctamente
## 20
## 0
Servicios de préstamos, amortización y pagos validados correctamente
## 20
## 0
Servicios de tarjetas, consumos, pagos y avances validados correctamente
## 20
## 0
Servicios de beneficiarios y transferencias validados correctamente
## 20
## 0
Servicios de cajero validados correctamente
## 20
## 0
Servicios de comercios y Hermes Pay validados correctamente
## 20
## 0
Servicios de correo desacoplados y reutilizables
## 20
## 0
Documentación, excepciones y logs
Documentación Swagger completa para endpoints, parámetros, body y respuestas
## 20
## 0
Swagger configurado para autenticación JWT
## 20
## 0
Global Exception Handler implementado correctamente
## 20
## 0
Respuestas de error utilizando Problem Details RFC 7807
## 20
## 0
Manejo centralizado de errores de negocio, validación y no encontrados
## 20
## 0
Serilog configurado en WebApp y WebAPI
## 20
## 0

Logs de operaciones financieras relevantes
## 20
## 0
Logs de errores no controlados con información útil
## 20
## 0
No registrar datos sensibles en logs, respuestas, vistas ni correos
## 20
## 0
Pruebas unitarias - Commands y Queries
Unit tests para Commands y Queries de Account
## 20
## 0
Unit tests para Commands y Queries de usuarios
## 20
## 0
Unit tests para Commands y Queries de préstamos
## 20
## 0
Unit tests para Commands y Queries de tarjetas de crédito
## 20
## 0
Unit tests para Commands y Queries de cuentas de ahorro
## 20
## 0
Unit tests para Commands y Queries de comercios
## 20
## 0
Unit tests para Commands y Queries de Hermes Pay
## 20
## 0
Unit tests para validadores FluentValidation
## 20
## 0
Pruebas unitarias - Servicios de negocio
Unit tests de servicios de cuentas y balances
## 20
## 0
Unit tests de servicios de transferencias y beneficiarios
## 20
## 0
Unit tests de servicios de depósitos y retiros
## 20
## 0
Unit tests de servicios de pagos a tarjetas
## 20
## 0
Unit tests de servicios de pagos a préstamos
## 20
## 0
Unit tests de cálculo de cuotas y tabla de amortización
## 20
## 0
Unit tests de servicios de tarjetas y avances de efectivo
## 20
## 0
Unit tests de servicios de comercios y procesamiento Hermes Pay
## 20
## 0
Unit tests de reglas de alto riesgo y validaciones financieras críticas
## 20
## 0
Pruebas de integración - Repositorios y persistencia
Integration tests de repositorios de usuarios, roles y tokens
## 20
## 0
Integration tests de repositorios de cuentas de ahorro
## 20
## 0
Integration tests de persistencia de transacciones financieras
## 20
## 0
Integration tests de repositorios de préstamos y amortización
## 20
## 0
Integration tests de repositorios de tarjetas y consumos
## 20
## 0
Integration tests de repositorios de beneficiarios
## 20
## 0
Integration tests de repositorios de comercios y usuarios de comercio
## 20
## 0

Integration tests de operaciones transaccionales con base de datos de prueba
## 20
## 0
Uso de InMemory Database o SQLite en memoria sin depender de BD real
## 20
## 0
Calidad final, entrega y ejecución
Solución compila correctamente sin errores
## 20
## 0
Migraciones aplican correctamente y generan la base de datos esperada
## 20
## 0
Seed de datos mínimos funcionales para pruebas iniciales
## 20
## 0
La WebApp ejecuta correctamente con sus módulos principales
## 20
## 0
La WebAPI ejecuta correctamente con Swagger disponible
## 20
## 0
Las pruebas automatizadas ejecutan correctamente desde la solución
## 200
Manejo adecuado de configuración mediante appsettings y ambientes
## 20
## 0
Código organizado, legible, mantenible y consistente
## 20
## 0
No exposición de datos sensibles en UI, API, correos ni logs
## 20
## 0
## Total44600
## Promedio200
## Porcentaje1000