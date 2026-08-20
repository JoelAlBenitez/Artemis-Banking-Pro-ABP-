# Artemis Banking Pro (ABP)

Plataforma bancaria digital construida con **ASP.NET Core MVC** y **.NET 9**. Incluye una aplicación web con tres perfiles de usuario (administrador, cajero y cliente), una Web API administrativa independiente y **Hermes Pay**, el procesador de pagos para comercios.

El sistema gestiona productos financieros —cuentas de ahorro, préstamos y tarjetas de crédito—, la operación diaria de caja, las transacciones de los clientes y el procesamiento de consumos con tarjeta.

**Grupo 5** · Proyecto final · ITLA

---

## Tabla de contenido

- [Qué hace el sistema](#qué-hace-el-sistema)
- [Equipo](#equipo)
- [Arquitectura](#arquitectura)
- [Tecnologías](#tecnologías)
- [Requisitos](#requisitos)
- [Puesta en marcha](#puesta-en-marcha)
- [Usuarios de prueba](#usuarios-de-prueba)
- [Web API](#web-api)
- [Reglas de negocio clave](#reglas-de-negocio-clave)
- [Pruebas](#pruebas)
- [Documentación](#documentación)

---

## Qué hace el sistema

### Administrador (WebApp)

Panel con indicadores generales del banco y la gestión completa de productos financieros.

- **Dashboard** — transacciones históricas y del día, pagos históricos y del día, clientes activos e inactivos, productos financieros activos y deuda promedio por cliente.
- **Usuarios** — creación de administradores, cajeros y clientes; validación de usuario, correo y cédula únicos; cuenta de ahorro principal automática al crear un cliente; activación e inactivación con bloqueo de auto-modificación.
- **Préstamos** — asignación con evaluación de alto riesgo, cuota bajo sistema francés, tabla de amortización, desembolso a la cuenta principal y edición de tasa que recalcula solo las cuotas futuras pendientes.
- **Tarjetas de crédito** — asignación, generación de número, expiración y CVC, edición de límite validando la deuda actual y cancelación solo sin deuda pendiente.
- **Cuentas de ahorro** — creación de cuentas secundarias y cancelación con transferencia automática del balance a la cuenta principal.

### Cajero (WebApp)

Operación de ventanilla con confirmación previa en cada movimiento e indicadores del día.

Depósitos · retiros · pago de tarjeta de crédito · pago de préstamo · transferencias a cuentas de terceros.

Los intentos rechazados quedan registrados en el historial sin afectar balances, y toda operación se atribuye al cajero autenticado.

### Cliente (WebApp)

- Home con productos financieros activos y detalle de cada uno.
- Beneficiarios, con validación de cuenta activa y que no sea propia.
- Transacción express, transferencia entre cuentas propias y transferencia a beneficiarios.
- Pago de tarjeta de crédito y de préstamo, sin permitir sobrepago.
- Avance de efectivo con interés del 6.25 %.

### Hermes Pay (Web API)

Procesador de pagos para comercios: valida número de tarjeta, expiración, CVC y crédito disponible; registra el consumo aprobado, aumenta la deuda de la tarjeta y acredita el pago en la cuenta principal del comercio. Los consumos rechazados se registran sin modificar balances ni deudas.

### Proceso automático

Una **Azure Function** con disparador por temporizador ejecuta el control de cuotas de préstamo atrasadas todos los días a las 5:00 AM. La expresión cron se configura en `LoansOverdueScheduleCron`.

---

## Equipo

| Matrícula | Integrante | Sección | Responsabilidad |
|---|---|:--:|---|
| 2025-1049 | Joel Alberto Benitez Varela | 10 | Módulo de Administrador y Web API completa |
| 2025-1150 | Adrian Francisco Brito Nelkitts | 10 | ASP.NET Identity, gestión de usuarios, autenticación y roles |
| 2025-1172 | Sebastian de Jesus Peguero Herrera | 10 | Módulo de Cliente |
| 2024-2174 | Robert Plaza Brito | 3 | Módulo de Cajero e interfaz completa de caja |

---

## Arquitectura

**Onion Architecture**. Las dependencias apuntan siempre hacia adentro: el dominio no conoce a nadie.

```
Source/
├── Core/
│   ├── ArtemisBankingPro.Core.Domain          Entidades, enums, contratos de repositorio,
│   │                                          errores de dominio y constantes de negocio
│   └── Artemis Banking Pro.Core.Application   Commands y Queries (CQRS), servicios de negocio
│                                              y genéricos, DTOs, ViewModels, AutoMapper,
│                                              Behaviors y excepciones tipadas
├── Infaestructure/
│   ├── ...Persistence                         EF Core, configuraciones y repositorios
│   ├── ...Identity                            ASP.NET Identity, JWT y siembra de datos
│   └── ...Shared                              Correo, generación de números y hash de CVC
├── Presentation/
│   ├── ...WebApp                              MVC con autenticación por cookies
│   ├── ...WebApi                              Controladores, Swagger y autenticación JWT
│   └── ...AzureFunction                       Control diario de cuotas atrasadas
├── ArtemisBankingPro.IOC                      Registro centralizado de dependencias
└── Tests/
    ├── ...Unit.Tests                          Commands, Queries y servicios de negocio
    └── ...Integration.Tests                   Repositorios y persistencia
```

**Decisiones transversales**

- **Sin borrado físico.** Toda baja es un cambio de estado, para conservar el historial aunque un producto o un usuario quede inactivo.
- **Atomicidad por confirmación única.** Cada operación que toca varias entidades se confirma en un solo `SaveChangesAsync`, que EF Core ejecuta dentro de su transacción implícita.
- **AutoMapper como única frontera** entre entidades, DTOs y ViewModels. Las vistas nunca reciben entidades de dominio.
- **Correos fuera de la transacción.** Un fallo de envío nunca revierte una operación financiera.
- **CQRS solo en la Web API.** La WebApp consume servicios de aplicación directamente; registrar los handlers desde ella arrastraría dependencias que solo existen en la API.
- **Datos sensibles nunca se exponen ni se registran:** número completo de tarjeta, CVC y tokens quedan fuera de vistas, respuestas, correos y logs.

---

## Tecnologías

| Área | Herramienta |
|---|---|
| Framework | .NET 9 · ASP.NET Core MVC · ASP.NET Core Web API |
| Datos | Entity Framework Core 9 (Code First) · SQL Server |
| Identidad | ASP.NET Core Identity · JWT Bearer |
| Patrones | MediatR 13 (CQRS) · Repositorio genérico y específico · Servicios genéricos y de negocio |
| Validación | FluentValidation 12, aplicada por un Behavior del pipeline de MediatR |
| Mapeo | AutoMapper 16 |
| Documentación | Swashbuckle (Swagger) con esquema de seguridad Bearer |
| Logging | Serilog, a consola y archivo con rotación diaria |
| Correo | MailKit / MimeKit |
| Pruebas | xUnit · Moq · FluentAssertions · EF Core InMemory |
| Serverless | Azure Functions (worker aislado, disparador por temporizador) |
| Interfaz | Bootstrap 5 · Bootstrap Icons · jQuery Validation |

**Manejo de errores.** Las excepciones tipadas de la capa de aplicación se traducen en un `IExceptionHandler` global a respuestas **Problem Details (RFC 7807)** con el código HTTP correcto: 400 para reglas de negocio y validación, 401, 403, 404 y 409 según el caso, y 500 con mensaje genérico para lo imprevisto. No hay `try/catch` por controlador.

**Trazabilidad.** Cada petición lleva un identificador de correlación, y los registros incluyen usuario, rol, acción y resultado.

---

## Requisitos

- **.NET SDK 9.0** o superior
- **SQL Server** — LocalDB, Express o una instancia completa
- **Visual Studio 2022** (17.12+) o VS Code con el SDK instalado
- **Azure Functions Core Tools v4** — solo si se va a ejecutar la function localmente

---

## Puesta en marcha

### 1. Configurar la conexión a la base de datos

La cadena de conexión vive en el `appsettings` del ambiente, no en el archivo base. Ajusta el servidor en:

```
Source/Presentation/ArtemisBankingPro.Presentation.WebApp/appsettings.Development.json
Source/Presentation/ArtemisBankingPro.Presentation.WebApi/appsettings.Development.json
```

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=TU_SERVIDOR;Initial Catalog=ArtemisBankingPro;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=60;MultipleActiveResultSets=True"
}
```

> El catálogo debe llamarse igual en los tres proyectos de presentación. Ambos contextos comparten base de datos pero llevan su propia tabla de historial de migraciones.

### 2. Configurar los secretos

Ni la clave de firma del JWT ni las credenciales de correo se versionan. Configúralas con **User Secrets** o variables de entorno:

```bash
cd "Source/Presentation/ArtemisBankingPro.Presentation.WebApi"
dotnet user-secrets set "JwtSettings:Key" "<clave de al menos 32 caracteres>"
dotnet user-secrets set "EmailSettings:SenderEmail" "<correo>"
dotnet user-secrets set "EmailSettings:Username"    "<correo>"
dotnet user-secrets set "EmailSettings:Password"    "<contraseña de aplicación>"
```

> La clave del JWT se valida al arranque. Si falta o es más corta de 32 caracteres, la API no levanta y el mensaje indica qué configurar. El proyecto trae una clave de desarrollo en `appsettings.Development.json` para que la revisión no requiera configuración adicional.

### 3. Aplicar las migraciones

Hay dos contextos y cada uno lleva sus propias migraciones:

```bash
dotnet ef database update --context DbContextArtemisBanking --project "Source/Infaestructure/ArtemisBankingPro.Infraestructrue.Persistence" --startup-project "Source/Presentation/ArtemisBankingPro.Presentation.WebApp"

dotnet ef database update --context IdentityContext --project "Source/Infaestructure/ArtemisBankingPro.Infraestructrue.Identity" --startup-project "Source/Presentation/ArtemisBankingPro.Presentation.WebApp"
```

Desde la Consola del Administrador de paquetes de Visual Studio:

```powershell
Update-Database -Context DbContextArtemisBanking
Update-Database -Context IdentityContext
```

### 4. Ejecutar

```bash
dotnet run --project "Source/Presentation/ArtemisBankingPro.Presentation.WebApp"   # https://localhost:7124
dotnet run --project "Source/Presentation/ArtemisBankingPro.Presentation.WebApi"   # https://localhost:7046
```

La raíz de la API redirige a **Swagger**. Los roles y los usuarios por defecto se siembran automáticamente al arrancar cualquiera de las dos aplicaciones.

> Al probar la API desde Visual Studio conviene usar **Ctrl+F5** (iniciar sin depurar). Las excepciones de negocio están capturadas por el manejador global y el cliente recibe un Problem Details limpio, pero el depurador se detiene igualmente en ellas.

---

## Usuarios de prueba

Sembrados automáticamente. Existen solo para facilitar la revisión del proyecto.

### Aplicación web

| Rol | Usuario | Contraseña |
|---|---|---|
| Administrador | `adminuser` | `Admin123*` |
| Cajero | `cajerouser` | `Cajero123*` |
| Cliente | `clienteuser` | `Cliente123*` |
| Cliente (prueba) | `prueba1` | `Prueba123*` |
| Cliente (prueba) | `prueba2` | `Prueba123*` |

### Web API

| Rol | Usuario | Contraseña |
|---|---|---|
| Administrador | `adminapi` | `AdminApi123*` |
| Comercio | `comercioapi` | `ComercioApi123*` |

Autentícate en `POST /account/login`, copia el token y pégalo en Swagger con el botón **Authorize**, con el formato `Bearer {token}`.

---

## Web API

31 endpoints en siete módulos, todos resueltos por MediatR. Salvo los de `account`, exigen JWT y rol.

| Módulo | Ruta base | Rol |
|---|---|---|
| Autenticación y cuenta | `/account` | Público |
| Usuarios | `/api/users` | Administrador |
| Préstamos | `/api/loan` | Administrador |
| Tarjetas de crédito | `/api/credit-card` | Administrador |
| Cuentas de ahorro | `/api/savings-account` | Administrador |
| Comercios | `/api/commerce` | Administrador |
| Hermes Pay | `/pay` | Administrador y Comercio |

El JWT incluye identificador de usuario, nombre de usuario, correo, rol, fecha de emisión y expiración. Un usuario inactivo no puede iniciar sesión ni obtener token.

---

## Reglas de negocio clave

| Regla | Valor |
|---|---|
| Interés de avance de efectivo | 6.25 % |
| Número de cuenta de ahorro | 9 dígitos como texto, único |
| Número de préstamo | 9 dígitos como texto, en rango disjunto del de cuentas |
| Número de tarjeta | 16 dígitos, único |
| Almacenamiento del CVC | Hash SHA-256, nunca en texto plano |
| Expiración de tarjeta | Fecha de emisión + 3 años |
| Paginación de listados | Máximo 20 registros por página, del más reciente al más antiguo |
| Token de restablecimiento | Vigencia de 30 minutos, un solo uso |
| Token de activación | Vigencia de 7 días, un solo uso |
| Precisión monetaria | `decimal(18,2)` |

Salidas de dinero se registran como **DÉBITO** y entradas como **CRÉDITO**. Las transferencias generan siempre el par cruzado, enlazado entre sí y confirmado junto a los balances en una sola operación.

---

## Pruebas

```bash
dotnet test "Artemis Banking Pro.sln"
```

**616 pruebas**, todas en verde:

- **475 unitarias** — Commands y Queries de los siete módulos, validadores de FluentValidation, el Behavior de validación, y los servicios de negocio: cuentas y balances, transferencias, beneficiarios, amortización, avances de efectivo, pagos, operaciones de caja, comercios y Hermes Pay.
- **141 de integración** — repositorios y persistencia sobre base de datos en memoria, sin depender de un servidor real.

---

## Documentación

| Documento | Contenido |
|---|---|
| `docs/Landing-Evaluacion-ABP.html` | Evaluación del proyecto criterio por criterio contra la rúbrica oficial, con la justificación de cada puntuación |
| `docs/PDFS/` | Documento funcional, rúbrica de evaluación, entidades y contratos base, y requerimientos externos |
| Swagger | `https://localhost:7046` — documentación viva de los 31 endpoints con sus parámetros, cuerpos y respuestas |

> La carpeta `docs/` está excluida del control de versiones por `.gitignore`, de modo que su contenido no viaja con el repositorio.
