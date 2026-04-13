# SunatGreApi

**SunatGreApi** es una Web API desarrollada en .NET 8 diseñada para gestionar, enriquecer y validar Guías de Remisión Electrónica (GRE) emitidas por la SUNAT (Perú). El sistema actúa como un middleware que recibe información de comprobantes electrónicos, extrae datos relevantes mediante procesamiento de texto y enriquece la información consultando sistemas externos (ERP/SQL Server) para facilitar procesos logísticos y contables.

## 🚀 Características Principales

- **Gestión de GRE:** Recepción de Guías de Remisión a través de endpoints REST.
- **Enriquecimiento de Datos:** 
  - Extracción automática de Números de Partida, Órdenes de Compra, Peso y Rollos desde la descripción de los bienes.
  - Sincronización con SQL Server para obtener códigos de tela, centros de costo, proveedores y tipos de movimiento.
- **Validación Robusta:** 
  - Verificación de campos obligatorios (Partida, Código de Tela, OC).
  - Validación de estados de orden y centros de costo autorizados.
  - Control de consistencia en cantidades y pesos.
- **Persistencia Dual:** 
  - **SQLite:** Almacenamiento local para el seguimiento de procesos y auditoría.
  - **SQL Server:** Integración con bases de datos externas para consultas de maestros y transacciones ERP.
- **Filtros Avanzados:** Búsqueda de guías por fecha, estado de proceso y etapa (Producción/Desarrollo).

## 🛠️ Tecnologías Utilizadas

- **Framework:** .NET 8 (ASP.NET Core Web API)
- **Base de Datos Local:** Entity Framework Core con SQLite.
- **Base de Datos Externa:** System.Data.SqlClient (SQL Server).
- **Documentación:** Swagger / OpenAPI.
- **Herramientas de Procesamiento:** Lógica personalizada de búsqueda difusa (Levenshtein Distance) para emparejamiento de nombres comerciales.

## 📁 Estructura del Proyecto

```text
SunatGreApi/
├── Controllers/       # Endpoints de la API (GuiaController)
├── Data/              # Contexto de base de datos local (AppDbContext)
├── Models/            # Modelos de datos y DTOs
├── Repositories/      # Acceso a datos externo (SQL Server)
├── Services/          # Lógica de negocio, mapeo y validación
├── Utils/             # Utilidades para parsing de strings y helpers
└── Program.cs         # Configuración y arranque de la aplicación
```

## ⚙️ Configuración

Asegúrese de configurar las cadenas de conexión en su entorno o archivo de configuración:

- `SqliteConnection`: Ruta al archivo `.db` de SQLite (por defecto `guias_sunat.db`).
- `SqlServerConnection`: Cadena de conexión a la base de datos externa para enriquecimiento.

## 📡 Endpoints Principales

### Guías (`/api/v1/Guia`)

| Método | Endpoint | Descripción |
| :--- | :--- | :--- |
| **GET** | `/api/v1/Guia` | Lista guías con paginación y filtros (fecha, estado, etapa). |
| **GET** | `/api/v1/Guia/{id}` | Obtiene el detalle de una guía específica por su ID de SUNAT. |
| **POST** | `/api/v1/Guia` | Registra una nueva guía, inicia el enriquecimiento y la validación. |
| **PUT** | `/api/v1/Guia/{id}` | Actualiza el estado de proceso de una guía. |
| **DELETE** | `/api/v1/Guia/{id}` | Elimina una guía del registro local. |

## 🧪 Lógica de Validación

El sistema clasifica las guías en diferentes estados de proceso:
- **PENDIENTE:** Guía registrada y lista para ser procesada.
- **OBSERVADO:** La guía falló en alguna validación técnica (ej. falta Partida o Código de Tela).
- **PROCESADO / COMPLETADO:** Guía que ha pasado satisfactoriamente todas las etapas.

## 📝 Notas de Implementación

- **Parsing de Descripción:** El sistema utiliza `SunatHelper` para extraer información estructurada de campos de texto libre enviados por SUNAT.
- **Tolerancia a Errores:** Se implementó la Distancia de Levenshtein para mitigar errores de tipeo en las descripciones de los productos al compararlos con el maestro de telas.

---
© 2026 SunatGreApi - Gestión Inteligente de Documentos Electrónicos.

## 🧪 Pruebas Unitarias

El proyecto incluye una suite de pruebas unitarias utilizando **xUnit** y **Moq** para validar la lógica de negocio en `GuiaService`.

Para ejecutar las pruebas localmente:

```bash
dotnet test
```

Las pruebas cubren escenarios como:
- Validación de reglas de exclusión (estado "BAJA", descripción con "TWILL").
- Manejo de duplicados.
- Actualización de estados de proceso.

## 🚀 Integración Continua (CI/CD)

Se ha configurado un flujo de trabajo de **GitHub Actions** (`.github/workflows/dotnet.yml`) que se activa automáticamente en cada `push` o `pull_request` a las ramas principales (`main`, `master`, `develop`).

El flujo realiza las siguientes tareas:
1.  Configura el entorno de .NET 8.
2.  Restaura las dependencias.
3.  Compila la solución.
4.  Ejecuta todas las pruebas unitarias.

## 📋 Plantilla de Pull Request

Se ha incluido una plantilla de Pull Request (`.github/pull_request_template.md`) para estandarizar las contribuciones y asegurar que cada cambio pase por una revisión y validación adecuada.
