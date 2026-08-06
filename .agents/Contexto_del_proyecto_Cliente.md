# Contexto del proyecto

Necesito que analices los siguientes documentos del proyecto **Artemis Banking Pro (ABP)** para comprender completamente la arquitectura, los requerimientos y las funcionalidades del sistema antes de generar cualquier documentación o código.

## Documentos principales

### 1. Proyecto final_ Artemis Banking Pro (ABP).pdf

Es el documento principal del proyecto.

Contiene:

* Requerimientos funcionales y no funcionales.
* Funcionalidades del sistema.
* Casos de uso.
* Reglas de negocio.
* Arquitectura general.
* Restricciones del proyecto.

> Si necesitas consultar rápidamente los requerimientos funcionales sin recorrer las aproximadamente 220 páginas del PDF, puedes utilizar el archivo **Proyecto_final_Artemis_Banking_Pro.txt**, que contiene el contenido textual del documento principal.

---

### 2. Evaluación Proyecto Final_ Artemis Banking Pro (ABP).pdf

Contiene la rúbrica oficial con la que será evaluado el sistema.

Esta rúbrica debe utilizarse para identificar:

* funcionalidades obligatorias,
* entregables,
* criterios de evaluación,
* módulos correspondientes a la funcionalidad de **Funcionalidades del Cliente**.

---

### 3. Entidades de Negocio y Contratos Base - ABP.pdf

Este documento contiene los elementos compartidos por todo el sistema.

Incluye, entre otros:

* Entidades
* DTOs
* Contratos
* Interfaces
* Servicios base
* Repositorios
* Objetos comunes
* Convenciones generales

Su objetivo es darte contexto sobre la arquitectura y conocer los elementos reutilizables antes de construir la documentación específica de la funcionalidad de Cliente.

No debes modificar ni redefinir estos elementos (`Transaction`, `Beneficiary`, `SavingsAccount`, `CreditCard`, `Loan`, `BaseEntity`, enums, DTOs base, ViewModels base, repositorio genérico, servicio genérico, `IUnitOfWork`, etc.), únicamente comprenderlos y utilizarlos como referencia.

---

### 4. Requerimientos Externos - Módulo Administración.pdf

Especifica los servicios que **mi módulo (Cliente)** debe **exponer** para que el módulo Administrador pueda operar (indicadores del Dashboard, deuda de clientes, etc.). No define implementaciones, solo contratos funcionales que mi módulo debe respetar al construir sus propios servicios.

---

### 5. Documentación técnica ya generada del módulo Administrador

Ya existe documentación de referencia (entidades, DTOs, servicios, convenciones) generada para el módulo de Administradores por otro integrante del equipo. **No la dupliques ni la reescribas** — úsala únicamente para entender entidades compartidas (`Transaction`, `SavingsAccount`, `CreditCard`, `Loan`) y convenciones transversales (no borrado físico, atomicidad, AutoMapper, paginación, Serilog).

---

# Funcionalidad que desarrollaré

Mi responsabilidad dentro del proyecto es exclusivamente el módulo de **Funcionalidades del Cliente**.

Dentro de este módulo existen las siguientes áreas:

* **Beneficiarios** — registrar, listar y eliminar (baja lógica) cuentas de otros clientes como beneficiarios frecuentes.
* **Transacciones** — Transacción Express, Pago a tarjeta de crédito, Pago a préstamo, Transacción a beneficiarios.
* **Avances de efectivo** — transferencia desde una tarjeta de crédito propia hacia una cuenta de ahorro propia, con interés del 6.25 %.
* **Transferencias entre cuentas propias** — movimiento de fondos entre cuentas de ahorro del mismo cliente.

## Módulos de otros integrantes (solo se consumen)

Existen dos excepciones importantes:

* La **gestión de usuarios** pertenece al proyecto **Identity** y la desarrolla otro integrante del equipo.
* La **gestión de préstamos, tarjetas de crédito y cuentas de ahorro** (asignación, cancelación, edición de límite/tasa) pertenece al módulo **Administrador** y la desarrolla otro integrante del equipo.

Esto significa que:

* las entidades `SavingsAccount`, `CreditCard`, `Loan`, `LoanInstallment` y los repositorios/servicios que las administran **ya existirán** (los define y mantiene el módulo Administrador, sobre entidades del dominio compartido);
* los datos de presentación del usuario (nombre, apellido, cédula, correo) **ya existirán** vía Identity;
* NO debo duplicar lógica de asignación, cancelación o edición de esos productos;
* NO debo recrear servicios existentes de Identity o Administrador;
* únicamente debo **consumirlos** (leer balance, deuda, estado activo/cancelado) cuando mi módulo lo necesite;
* solamente debo desarrollar la lógica propia de mis 4 áreas: creación/eliminación de beneficiarios, y las operaciones que generan `Transaction` (débito/crédito) sobre cuentas, tarjetas y préstamos ya existentes.

---

# Objetivo

Necesito que construyas documentación técnica en formato Markdown (.md) que sirva como guía para desarrollar posteriormente cada funcionalidad de mi módulo.

La documentación debe estar organizada de forma que pueda utilizarse como referencia rápida durante todo el desarrollo.

---

# Organización de los archivos Markdown

Dentro del directorio **docs** ya existe la estructura de carpetas correspondiente a mi módulo (`Client Home`, `Beneficiaries`, `Transactions`, `CashAdvance`, `AccountTransfer`).

Cada funcionalidad posee su propio subfolder.

Debes generar múltiples archivos Markdown pequeños, distribuidos por funcionalidad. Ejemplos:

* acciones
* métodos
* DTOs
* entidades
* servicios
* repositorios
* validaciones
* reglas de negocio
* procesos
* endpoints
* cualquier otro elemento que pertenezca exclusivamente a dicha funcionalidad.

No debes concentrar demasiada información en un único archivo.

---

# Tamaño de cada Markdown

Cada archivo debe ser pequeño.

El objetivo es que:

* pueda consultarse rápidamente;
* facilite la navegación;
* permita dividir el desarrollo en pequeñas tareas.

Cada Markdown debe representar aproximadamente un conjunto de trabajo equivalente a **menos de 10 cambios antes de realizar un commit**.

Esto permitirá:

* mantener commits pequeños;
* conservar la trazabilidad;
* facilitar la continuidad del trabajo si la conversación termina;
* minimizar la pérdida de contexto cuando se agoten los tokens.

---

# Fuente de la información

Toda la documentación debe basarse únicamente en los documentos proporcionados.

No debes:

* asumir funcionalidades;
* inventar reglas de negocio;
* agregar procesos no descritos;
* completar información utilizando conocimientos externos.

Si algún elemento no aparece explícitamente en la documentación, debe indicarse como **pendiente** o **No especificado**.

---

# Funcionalidades pertenecientes a otros módulos

Durante el análisis encontrarás métodos o procesos que pertenecen a otras funcionalidades (Identity, Administrador) pero que son requeridos indirectamente por mi módulo (por ejemplo: validar que una cuenta/tarjeta/préstamo esté activo, obtener nombre y apellido de un cliente).

Estos métodos deben documentarse únicamente como referencia.

No deben mezclarse con la documentación principal de mi módulo.

Para ello, ya existe un subfolder independiente de dependencias externas donde se documentan estas dependencias.

---

# Legibilidad

Toda la documentación y cualquier código que posteriormente se genere deben priorizar:

* claridad;
* simplicidad;
* facilidad de mantenimiento;
* facilidad de lectura.

La intención es que posteriormente pueda continuar el desarrollo manualmente sin depender completamente del agente de IA.

---

# Restricciones técnicas transversales obligatorias

Además de las reglas de negocio del documento funcional, toda documentación y código que se genere para mi módulo debe respetar:

* **Onion Architecture:** los controladores de la WebApp nunca contienen lógica financiera ni consultan repositorios directamente; toda regla de negocio vive en la capa de aplicación.
* **Atomicidad real:** toda operación que modifica más de un balance o entidad relacionada se ejecuta dentro de una transacción explícita de Entity Framework Core (`IUnitOfWork` / `IDbContextTransaction`), nunca con `SaveChangesAsync` sueltos.
* **Anti-sobrepago:** en pagos a tarjeta y a préstamo, si el monto ingresado supera la deuda/pendiente real, solo se debita el monto efectivo; el excedente se descarta.
* **Restricciones de Serilog:** las operaciones financieras generan logs informativos, pero nunca se registran números completos de tarjeta, CVC, contraseñas ni tokens.
* **Escalabilidad:** operaciones asíncronas, paginación obligatoria (máx. 20 registros), proyección a DTO en la consulta (`ProjectTo`/`IQueryable`), servicios sin estado.

---

# Exclusiones del desarrollo

Los siguientes componentes NO serán desarrollados por el agente de IA:

* Azure Functions.
* Pruebas unitarias.
* Pruebas de integración.
* Interfaz de usuario (UI).

Estos elementos únicamente deben mencionarse cuando aparezcan en la documentación del proyecto.

En cada Markdown correspondiente deben quedar claramente marcados como:

**No desarrollado por el agente de IA (implementación realizada manualmente).**

No debes generar planificación detallada ni código relacionado con estos componentes.

---

# Instrucciones finales

Antes de generar cualquier Markdown o código:

1. Analiza completamente todos los documentos.
2. Comprende la arquitectura general y las entidades compartidas ya definidas.
3. Identifica únicamente lo relacionado con el módulo de Funcionalidades del Cliente.
4. Detecta las dependencias con Identity y con el módulo Administrador.
5. Organiza la documentación siguiendo la estructura descrita anteriormente.
6. No comiences a generar archivos hasta haber terminado el análisis completo de la documentación.
