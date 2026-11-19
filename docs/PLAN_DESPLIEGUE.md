# Plan de Despliegue en la Nube con Docker Multi-Stage (Vercel, Render y Supabase)
## Sistema GIATEP · Certamen 3 (Hito 3)

---

## 1. Arquitectura de Entornos y Topología de Red

El despliegue de producción de GIATEP adopta una arquitectura desacoplada, elástica y distribuida en la nube, eliminando la dependencia de servidores locales físicos y aprovechando plataformas especializadas para cada capa del sistema:

```
+-------------------------------------------------------------------------------+
|                             CLIENTES Y NAVEGADORES                            |
+-------------------------------------------------------------------------------+
           |                                                       |
   HTTPS / CDN Edge                                        HTTPS / API REST
           v                                                       v
+-------------------------------+               +-------------------------------+
|      VERCEL EDGE NETWORK      |               |      RENDER WEB SERVICES      |
|    (SPA React 19 / Vite)      |               |     (Docker Multi-Stage)      |
|   https://giatep.vercel.app   |               |  https://giatep-api.onrender  |
|                               |               |                               |
| - Rewrites SPA (vercel.json)  |               | - ASP.NET Core (.NET 10)      |
| - Compresión Brotli / Gzip    |               | - Non-root USER app (8080)    |
| - HSTS y HTTP/2 forzado       |               | - Cloudflare Reverse Proxy    |
+-------------------------------+               +-------------------------------+
                                                                |
                                                      TCP 5432 (Pooler)
                                                                v
                                                +-------------------------------+
                                                |     SUPABASE POSTGRESQL 17    |
                                                | aws-1-sa-east-1.pooler...:5432|
                                                |                               |
                                                | - 18 Tablas Relacionales      |
                                                | - Connection Pooling (Session)|
                                                | - Backups automáticos / PITR  |
                                                +-------------------------------+
```

### 1.1 Entornos Definidos
1. **Entorno de Desarrollo Local (`Development`)**:
   - Backend ejecutado con `dotnet run --project backend/Giatep.Api`.
   - Frontend ejecutado con `npm run dev` en `http://localhost:5173`.
   - Soporte dual de bases de datos: PostgreSQL local o SQL Server Express (`GiatepDB`).
   - Herramientas interactivas OpenAPI / Scalar habilitadas en `/scalar/v1`.
2. **Entorno de Integración Continua (`CI / Test`)**:
   - Ejecutado en GitHub Actions sobre runners `ubuntu-latest`.
   - Compilación estricta sin advertencias, ejecución de 72 pruebas automáticas en `Giatep.Tests` y empaquetado de artefactos.
3. **Entorno de Producción en la Nube (`Production`)**:
   - **Capa Web**: Vercel Edge Network (`https://giatep.vercel.app`).
   - **Capa de Servicios**: Render Web Service (`https://giatep-api.onrender.com`).
   - **Capa de Persistencia**: Supabase PostgreSQL 17 (`aws-1-sa-east-1.pooler.supabase.com:5432`).
   - Endpoints de depuración e interfaces OpenAPI desactivados; errores comunicados exclusivamente mediante Problem Details RFC 7807/9110 sanitizados.

---

## 2. Construcción del Contenedor Docker Multi-Stage

El backend se empaqueta en una imagen de contenedor compatible con Open Container Initiative (OCI) mediante un `Dockerfile` optimizado en dos etapas (Multi-Stage Build):

```dockerfile
# ==============================================================================
# Etapa 1: Compilación y empaquetado (SDK completo de .NET 10)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivos de definición de proyectos para cacheo de capas NuGet
COPY backend/Giatep.Api/Giatep.Api.csproj backend/Giatep.Api/
COPY backend/Giatep.slnx backend/
RUN dotnet restore backend/Giatep.Api/Giatep.Api.csproj

# Copiar el código fuente completo y compilar en Release
COPY backend/Giatep.Api/ backend/Giatep.Api/
WORKDIR /src/backend/Giatep.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ==============================================================================
# Etapa 2: Imagen de ejecución productiva (Runtime liviano y seguro)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Creación de carpetas de almacenamiento local de evidencias
RUN mkdir -p /app/storage/evidencias && \
    mkdir -p /app/storage/firmas

# Copiar exclusivamente los binarios compilados desde la etapa de build
COPY --from=build /app/publish .

# Seguridad: Ejecutar bajo el usuario predefinido sin privilegios root
USER app

# Exposición del puerto de red interno
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Punto de entrada de la aplicación
ENTRYPOINT ["dotnet", "Giatep.Api.dll"]
```

### 2.1 Beneficios del Enfoque Multi-Stage
- **Superficie de Ataque Mínima**: La imagen final contiene únicamente los binarios compilados y el runtime de ASP.NET Core. No incluye compiladores, depuradores ni código fuente.
- **Peso Reducido**: Tamaño de imagen inferior a 250 MB, permitiendo descargas rápidas en despliegues automatizados.
- **Principio de Mínimos Privilegios**: Al correr bajo `USER app` (UID no root predefinido en las imágenes oficiales de Microsoft en .NET 10), se previene cualquier intento de escalamiento de privilegios hacia el kernel del servidor anfitrión.

---

## 3. Variables de Entorno de Producción

Ninguna credencial ni secreto se encuentra almacenado en el repositorio Git. La configuración se inyecta en tiempo de ejecución a través de los paneles de administración de cada servicio en la nube:

### 3.1 Variables en Render (Backend API)

| Variable de Entorno | Valor / Descripción | Propósito |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Activa modo de producción: suprime trazas de depuración, desactiva Swagger/Scalar y fuerza políticas de seguridad. |
| `PORT` | `8080` | Puerto asignado por Render enlazado a `0.0.0.0:$PORT`. |
| `ConnectionStrings__Giatep` | `Server=aws-1-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;User Id=postgres.hznzllesjzsbofxadywi;Password=...;Ssl Mode=Require;Trust Server Certificate=true;Pooling=true;` | Cadena de conexión al pooler transaccional de Supabase PostgreSQL 17 con SSL obligatorio. |
| `Jwt__Key` | *(Clave criptográfica de más de 32 bytes en Base64/Hex)* | Llave secreta para la firma y verificación simétrica de tokens JWT con HMAC-SHA256. |
| `Jwt__Issuer` | `GiatepBackend` | Emisor oficial de los tokens JWT del sistema. |
| `Jwt__Audience` | `GiatepFrontend` | Audiencia destinataria autorizada para consumir los tokens. |
| `Ocr__Engine` | `OcrSpace` | Motor activo de reconocimiento óptico de caracteres para digitalización de documentos. |
| `Ocr__ApiKey` | *(Clave privada de API provista por OCR.space)* | Clave para autenticación ante el servicio neuronal de OCR.space (redactada de logs). |
| `Storage__Path` | `/app/storage/evidencias` | Directorio local dentro del contenedor para la persistencia de evidencias y recortes temporales (con fallback resiliente a `/tmp` si no hay permisos). |

### 3.2 Variables en Vercel (Frontend SPA)

| Variable de Entorno | Valor de Producción | Propósito |
|---|---|---|
| `VITE_API_URL` | `https://giatep-api.onrender.com` | URL base pública de la API desplegada en Render para todas las llamadas AJAX/Fetch. |

---

## 4. Procedimiento de Despliegue y Migraciones en Supabase

### 4.1 Despliegue de la Base de Datos en Supabase
1. Acceder al proyecto de Supabase (`hznzllesjzsbofxadywi`) en la región Sudamérica Este (São Paulo, `aws-1-sa-east-1`).
2. Ejecutar el script DDL canónico `backend/schema_postgres.sql` en el SQL Editor de Supabase:
   - Crea las 18 tablas normalizadas.
   - Aplica las restricciones CHECK y claves foráneas con `ON DELETE RESTRICT`.
   - Crea los índices compuestos (`IX_Casos_Estado_Fecha`) y filtrados (`IX_Hechos_Investigacion_EsFinal`).
3. Verificar que la cuenta de conexión del pooler cuente con permisos plenos de lectura y escritura (`DML/DDL`) sobre el esquema `public`.

### 4.2 Despliegue Continuo del Backend en Render
1. El servicio web en Render está enlazado a la rama `main` del repositorio de GitHub.
2. Ante cada `git push` a `main`:
   - Render descarga el código y detecta el `Dockerfile` en la raíz.
   - Construye la imagen multi-stage (.NET 10 SDK -> Runtime).
   - Inicia el contenedor exponiendo el puerto configurado en `$PORT`.
   - Ejecuta una sonda de salud contra `GET /health` antes de conmutar el tráfico de red, garantizando **Zero-Downtime Deployment**.

### 4.3 Despliegue Continuo del Frontend en Vercel
1. El proyecto de Vercel está vinculado al repositorio con Root Directory configurado en `frontend`.
2. Ante cada `git push`:
   - Vercel ejecuta `npm run build` (`tsc -b && vite build`).
   - Despliega el bundle estático en la red global de borde (Edge Network).
   - Aplica las reglas de `frontend/vercel.json`:
     ```json
     {
       "rewrites": [
         {
           "source": "/(.*)",
           "destination": "/index.html"
         }
       ]
     }
     ```
     asegurando que cualquier ruta de navegación de React Router (ej. `/casos`, `/dashboard`, `/investigaciones`) reciba el `index.html` y no retorne error 404 al recargar el navegador.

---

## 5. Verificación de Salud, Monitoreo y Métricas

### 5.1 Endpoint de Verificación Activa (`GET /health`)
El backend cuenta con un endpoint de salud diseñado para monitores automatizados externos (UptimeRobot, Better Uptime o sondas internas de Render):
- **Método**: `GET` (Obligatorio; solicitudes `HEAD` reciben 401 debido a la FallbackPolicy de autenticación).
- **URL**: `https://giatep-api.onrender.com/health`
- **Respuesta Esperada (HTTP 200 OK)**:
  ```json
  {
    "estado": "disponible",
    "almacenamiento": "Supabase PostgreSQL"
  }
  ```
- **Lógica Interna**: Ejecuta una consulta ligera de conectividad (`SELECT 1` / `Database.CanConnectAsync()`) contra Supabase PostgreSQL. Si la conexión falla, retorna inmediatamente HTTP 503 Service Unavailable bajo el estándar Problem Details.

### 5.2 Mitigación de Inicios en Frío (Render Cold Start)
Debido a que el nivel gratuito de Render suspende los contenedores tras 15 minutos de inactividad:
- El cliente frontend en `frontend/src/api/client.ts` configura un timeout de 60 segundos (`AbortSignal.timeout(60000)`) para esperar pacientemente el arranque del contenedor sin fallar.
- **Protocolo de Presentación**: Para la defensa oral, el expositor debe ejecutar una llamada GET a `/health` 5 minutos antes de la sesión, asegurando que el contenedor permanezca caliente y responda de forma instantánea (< 200 ms).

---

## 6. Respaldo, Continuidad Operativa y Rollback

### 6.1 Estrategia de Respaldo en Supabase
- **Respaldos Automáticos Diarios**: Supabase ejecuta snapshots completos diarios de la base de datos PostgreSQL.
- **Point-in-Time Recovery (PITR)**: Permite restaurar el estado de la base de datos a cualquier segundo específico en los últimos 7 días ante corrupciones de datos o eliminaciones no deseadas.
- **Métricas Operativas**:
  - **RPO (Recovery Point Objective)**: Menor a 1 hora (o punto exacto en el tiempo con PITR).
  - **RTO (Recovery Time Objective)**: Menor a 30 minutos para aprovisionamiento de una nueva instancia con los datos restaurados.

### 6.2 Estrategia de Rollback
1. **Rollback de Frontend (Vercel)**:
   - Vercel almacena instantáneamente el historial inmutable de cada despliegue.
   - Si un release de frontend introduce una anomalía visual, se pulsa "Instant Rollback" en el panel de Vercel, redirigiendo el 100% del tráfico al despliegue anterior en menos de 2 segundos.
2. **Rollback de Backend (Render)**:
   - Si una nueva versión del backend falla las pruebas de humo en producción, Render permite seleccionar la imagen previa en la pestaña "Deploys" y activar un "Rollback", restaurando el contenedor anterior de inmediato.
3. **Rollback de Base de Datos**:
   - Para cambios en el esquema relacional, se diseñan scripts de migración simétricos (Up y Down). Las operaciones destructivas (como eliminación de columnas) se ejecutan solo tras validar que el software no depende del esquema previo.

---

## 7. Seguridad Operacional en Producción

1. **Protección Contra Ataques DoS / Abuso de Recursos**:
   - 4 limitadores de tasa activos: 240 req/min global, 20 req/min en login, 30 req/min en exportación y 6 req/min en OCR.
   - Peticiones excedentes reciben HTTP 429 Too Many Requests.
2. **Cabeceras de Protección HTTP Inyectadas Globalmente**:
   - `X-Content-Type-Options: nosniff` (previene MIME sniffing).
   - `X-Frame-Options: DENY` (previene Clickjacking).
   - `Referrer-Policy: no-referrer` (evita fuga de rutas en encabezados).
   - `Cache-Control: no-store` (impide almacenamiento de datos de salud en cachés públicas).
3. **Sanitización de Archivos y Prevención de Path Traversal**:
   - Comprobación de magic bytes en subidas (PDF, PNG, JPEG).
   - Cálculo y validación de hash SHA-256 en almacenamiento y descarga; archivos alterados en disco disparan alarma inmediata.
   - Rutas resueltas estrictamente dentro del subdirectorio de almacenamiento asignado, rechazando intentos de secuencias `../`.

---

## 8. Lista de Verificación Previa a Producción (Checklist)

- [x] Contenedor Docker Multi-Stage compila limpiamente en Release sin advertencias.
- [x] Contenedor configurado para correr bajo usuario no root `USER app` en puerto 8080.
- [x] Base de datos Supabase PostgreSQL 17 aprovisionada con las 18 tablas del esquema `schema_postgres.sql`.
- [x] Variables de entorno productivas inyectadas en Render y Vercel sin credenciales en Git.
- [x] Endpoint `GET https://giatep-api.onrender.com/health` responde HTTP 200 OK reportando Supabase PostgreSQL.
- [x] Frontend en `https://giatep.vercel.app` enruta correctamente y soporta refrescos SPA.
- [x] 72 pruebas automáticas en `Giatep.Tests` ejecutadas y aprobadas en el pipeline de CI/CD.
- [x] Auditoría de accesibilidad axe-core con 0 infracciones en todas las vistas principales.
- [x] Plan de contingencia, RPO/RTO y procedimientos de rollback formalmente documentados.
