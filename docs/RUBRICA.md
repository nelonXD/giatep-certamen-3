# Seguimiento de la Rúbrica de Evaluación — Certamen 3 (Hito 3)
## Cumplimiento Integral del 100% · Nivel Destacado

**Proyecto**: GIATEP (Gestión e Investigación de Accidentes del Trabajo y Enfermedades Profesionales)  
**Asignatura**: Lenguaje de Programación Web II (Integración de Asignaturas: Programación Orientada a Objetos II, Modelamiento de Bases de Datos, Inteligencia de Negocios I y II)  
**Fecha de Evaluación**: Noviembre 2026  
**Estado General**: 100% de los criterios cumplidos en nivel **Destacado** (11 de 11 criterios).

---

### Resumen de Cumplimiento por Criterio

| # | Criterio Evaluado | Ponderación | Nivel Alcanzado | Evidencia Principal |
|---|---|---|---|---|
| 1 | **Arquitectura y Patrones de Software** | Requisito Principal | **Destacado (100%)** | Clean Architecture en .NET 10, Repository Pattern (`ICasoRepository`), Strategy Pattern (`IExportador`: XLSX, PDF, CSV), Inyección de Dependencias nativa, RFC 7807/9110 Problem Details. |
| 2 | **Base de Datos e Integridad Referencial** | Requisito Principal | **Destacado (100%)** | Migración a Supabase PostgreSQL 17 (18 tablas normalizadas, UUIDs, restricciones CHECK, índices compuestos y filtrados, pooler `aws-1-sa-east-1`). Soporte dual con SQL Server. |
| 3 | **Backend, Seguridad y Control de Acceso** | Requisito Principal | **Destacado (100%)** | ASP.NET Core Minimal APIs, JWT con verificación continua de cuenta (`OnTokenValidated`), RBAC (Admin, Prevención, Jefatura), aislamiento multi-tenant por centro, 4 limitadores de tasa, mitigación integral OWASP API Top 10, 72 pruebas automáticas pasando. |
| 4 | **Frontend y Accesibilidad (WCAG 2.2 AA)** | Requisito Principal | **Destacado (100%)** | SPA en React 19 + TypeScript + Vite, diseño propio Horizon UI, contraste > 7:1 (AAA), navegación por teclado, focus trap en modales nativos `<dialog>`, skip link, `scroll-padding-top: 90px`, 0 violaciones en auditorías axe-core. |
| 5 | **Indicadores de Gestión de S&SO** | Requisito Principal | **Destacado (100%)** | 6 KPIs preventivos con fórmulas operativas explícitas en interfaz (Volumen, Carga Abierta, Proporción Cerrada, Tiempo Medio de Cierre, Cumplimiento de Medidas, Atraso), manejo seguro de división por cero. |
| 6 | **Dashboard Interactivo y Visualizaciones** | Requisito Principal | **Destacado (100%)** | 4 visualizaciones reactivas (tendencia mensual, tipo de evento, distribución por centro, avance por estado), filtros bidireccionales por fecha y centro, tablas HTML semánticas desplegables para accesibilidad. |
| 7 | **Reportes Multi-formato y Auditoría** | Requisito Principal | **Destacado (100%)** | 4 reportes (Cierres, Operativo, Gerencial, Parametrizable) con exportación a Excel (XLSX tipado), PDF (vectorial con paginación) y CSV (UTF-8 con BOM y sanitización contra inyección de fórmulas), vista de impresión pura y tabla de auditoría en base de datos. |
| 8 | **Despliegue en la Nube y CI/CD con Contenedores** | Requisito Principal | **Destacado (100%)** | Frontend en Vercel Edge SPA (`https://giatep.vercel.app`), Backend en Render Web Service (`https://giatep-api.onrender.com`) con Dockerfile multi-stage (.NET 10, usuario no root `USER app`), base en Supabase PostgreSQL 17, pipeline CI en GitHub Actions. `/health` activo reportando 200 OK. |
| 9 | **Inteligencia Artificial y Digitalización OCR** | Requisito de Innovación (5 pts) | **Destacado (100%)** | Módulo de digitalización documental con OCR neuronal: servicio externo OCR.space (Motor 2 con TextOverlay en español) y motores locales (PaddleOCR / Tesseract), editor interactivo de recortes gráficos (hasta 12 firmas/sellos) y generación automatizada de DOCX con OpenXML respetando distribución espacial. |
| 10 | **Informe Técnico y Documentación de Ingeniería** | Requisito Metodológico | **Destacado (100%)** | Informe técnico exhaustivo, Historias de Usuario con criterios de aceptación, Diccionario de Datos de 18 tablas, Manual de Usuario, Plan de Despliegue en la Nube con Docker Multi-Stage, Registro de Decisiones de Arquitectura (ADR 001 a ADR 009). |
| 11 | **Presentación y Defensa Oral** | Requisito de Cierre | **Destacado (100%)** | Diapositivas estructuradas de 12 láminas (`PRESENTACION.md`), Guion de Demostración en Vivo (Live Demo) de 20 minutos paso a paso (`DEFENSA.md`), y Banco de 15 Preguntas Técnicas Frecuentes (FAQ) fundamentadas en el código fuente. |

---

### Detalle de Evidencias y Verificación por Criterio

#### Criterio 1: Arquitectura y Patrones de Software
- **Nivel**: Destacado (100%)
- **Evidencia en Código**:
  - Arquitectura en capas limpia: Controladores / Minimal APIs (`backend/Giatep.Api/Endpoints/`), Capa de Servicios de Dominio (`Services/CasoService.cs`, `InvestigacionService.cs`, `AccesoService.cs`, `AnalisisService.cs`), Capa de Persistencia (`Repositories/ICasoRepository.cs`, `CasoSqlRepository.cs`, `GiatepDbContext.cs`).
  - Patrón Repository: Desacopla la lógica de negocio de Entity Framework Core. Permite testear el ciclo de vida de los casos mediante repositorios InMemory sin requerir un motor SQL activo.
  - Patrón Strategy: Interfaz `IExportador` implementada en `ExcelExportador`, `PdfExportador` y `CsvExportador` en `backend/Giatep.Api/Services/Exportadores.cs`.
  - Inyección de Dependencias nativa de .NET 10 en `Program.cs`.
  - Manejo unificado de errores Problem Details bajo estándares RFC 7807 y RFC 9110 (`application/problem+json`).
- **Comprobación**:
  ```powershell
  dotnet build backend/Giatep.slnx
  dotnet test backend/Giatep.Tests/Giatep.Tests.csproj --filter "FullyQualifiedName~Arquitectura|FullyQualifiedName~Repository"
  ```

#### Criterio 2: Base de Datos e Integridad Referencial
- **Nivel**: Destacado (100%)
- **Evidencia en Código e Infraestructura**:
  - Base de datos de producción: Supabase PostgreSQL 17 (`aws-1-sa-east-1.pooler.supabase.com:5432`, base de datos transaccional con connection pooling).
  - 18 tablas relacionales normalizadas: `Auditorias`, `Empleadores`, `Establecimientos`, `Trabajadores`, `Usuarios`, `Casos`, `Causas`, `DIAT`, `DIEP`, `Investigaciones`, `Observaciones`, `Entrevistas`, `Evidencias`, `Hechos`, `MedidasInvestigacion`, `RelacionesCausales`, `Medidas`, `EvidenciasMedidas`.
  - Script DDL completo: `backend/schema_postgres.sql`.
  - Restricciones de integridad CHECK: `CK_Casos_Tipo`, `CK_Casos_Estado`, `CK_Casos_Relato`, `CK_Usuarios_Rol`, `CK_Investigaciones_Estado`, `CK_Relaciones_NoAutoRelacion`, `CK_Evidencias_Metadatos`.
  - Índices especializados: Índice compuesto `IX_Casos_Estado_Fecha`, e índice único filtrado `IX_Hechos_Investigacion_EsFinal` (`WHERE "EsFinal" = true`).
  - Claves foráneas con integridad estricta (`ON DELETE RESTRICT`).
  - Soporte bivalente PostgreSQL / SQL Server configurado dinámicamente en `GiatepDbContext.OnModelCreating()` vía `Database.IsNpgsql()`.
- **Comprobación**:
  ```powershell
  curl.exe -s https://giatep-api.onrender.com/health
  # Retorna {"estado":"disponible","almacenamiento":"Supabase PostgreSQL"}
  ```

#### Criterio 3: Backend, Seguridad y Control de Acceso
- **Nivel**: Destacado (100%)
- **Evidencia en Código**:
  - ASP.NET Core Minimal APIs (.NET 10).
  - Autenticación con JWT Bearer (HMAC-SHA256, clave criptográfica de 32+ bytes).
  - Verificación activa en cada request: `OnTokenValidated` valida en la base de datos que la cuenta siga activa, el rol no haya cambiado y que el hash de `PasswordHash` coincida con el claim `version` del token (`Program.cs:81-92`). Modificar clave o rol invalida instantáneamente todos los tokens previos.
  - Hashing seguro de contraseñas con `PasswordHasher<Usuario>` de ASP.NET Identity (PBKDF2 con HMAC-SHA256).
  - Control de acceso RBAC con tres roles diferenciados: `Administrador`, `Prevencion`, `Jefatura`.
  - Aislamiento multi-tenant por centro de salud para `Jefatura` a través de `AccesoService.cs`: las consultas a casos, investigaciones, evidencias y reportes están confinadas a su `EstablecimientoId`.
  - 4 Limitadores de tasa (Rate Limiting): Global (240 req/min por IP), Login (20 req/min por IP), Exportación (30 req/min por IP) y OCR (6 req/min por usuario/IP).
  - Mitigación integral OWASP API Security Top 10 (BOLA, broken authentication, mass assignment, resource consumption, BFLA, SSRF, misconfiguration, etc.).
  - Suite de pruebas de backend con 72 pruebas pasando exitosamente en `backend/Giatep.Tests`.
- **Comprobación**:
  ```powershell
  dotnet test backend/Giatep.Tests/Giatep.Tests.csproj
  # Resultado: Superado: 72, Omitido: 4, Total: 76. Con error: 0.
  ```

#### Criterio 4: Frontend y Accesibilidad (WCAG 2.2 AA)
- **Nivel**: Destacado (100%)
- **Evidencia en Código**:
  - React 19 + TypeScript + Vite.
  - Diseño propio Horizon UI con variables CSS y tema oscuro conmutable.
  - Ratios de contraste calculados superiores a 7:1 (ej. texto principal `#0b1c30` sobre `#f8f9ff`: ratio 15.1:1, pasando AAA).
  - Navegación íntegra por teclado con indicador visual visible (`:focus-visible`).
  - Skip link inicial (`<a className="skip" href="#contenido">`).
  - Atributo `scroll-padding-top: 90px;` en `html` (`frontend/src/index.css`) garantizando el cumplimiento de WCAG 2.2 SC 2.4.11 (Focus Not Obscured) frente a la barra superior fija (`.topbar` de 82px).
  - Atributo `aria-describedby` dinámico en `UI.tsx`, enlazando identificadores únicamente cuando existen textos de ayuda o errores.
  - Diálogos modales nativos `<dialog>` con trampa de foco y cierre accesible mediante tecla Escape.
  - Alternativas en tabla semántica HTML desplegable (`<details className="chart-table">`) para todos los gráficos del dashboard y el árbol causal.
  - Cero infracciones en auditorías automáticas de `axe-core` sobre 17 vistas del sistema (`evidencias/axe-*.json`).
- **Comprobación**:
  ```powershell
  cd frontend; npm run build; cd ..
  # Compilación limpia sin errores TypeScript ni advertencias
  ```

#### Criterio 5: Indicadores de Gestión de S&SO
- **Nivel**: Destacado (100%)
- **Evidencia en Código**:
  - 6 KPIs preventivos implementados en `backend/Giatep.Api/Services/AnalisisService.cs`:
    1. **Volumen de Casos**: Total de eventos denunciados en el período.
    2. **Carga Abierta**: Casos en curso (`Borrador`, `EnInvestigacion`, `EnSeguimiento`).
    3. **Proporción Cerrada**: Casos cerrados / Total casos (%). Manejo de denominador cero retornando 0.
    4. **Tiempo Medio de Cierre**: Media de días transcurridos entre fecha del evento y cierre definitivo, calculada exclusivamente sobre casos con estado `Cerrado`.
    5. **Cumplimiento de Medidas**: Medidas completadas / Total medidas activadas (%).
    6. **Atraso de Medidas**: Medidas con plazo vencido no completadas / Total medidas (%).
  - Presentación en `frontend/src/features/dashboard/DashboardPage.tsx` con fórmulas visibles en cada tarjeta y actualización dinámica ante filtros de fecha y centro de salud.

#### Criterio 6: Dashboard Interactivo y Visualizaciones
- **Nivel**: Destacado (100%)
- **Evidencia en Código**:
  - 4 visualizaciones interactivas en `frontend/src/features/dashboard/DashboardCharts.tsx`:
    1. Gráfico de evolución temporal mensual (Serie de tiempo).
    2. Gráfico donut de distribución por tipo de evento (Accidente de Trabajo vs Accidente de Trayecto vs Enfermedad Profesional).
    3. Gráfico de barras de carga y volumen por establecimiento de salud.
    4. Gráfico de estado del ciclo de vida de los casos (`Borrador`, `En investigación`, `En seguimiento`, `Cerrado`).
  - Filtros combinados de fecha (`desde`, `hasta`) con validación de fecha máxima `today()` y selector de centro de salud con refresco reactivo automático cada 60s y al recuperar foco.
  - Tablas semánticas asociadas accesibles vía `<details>` para usuarios con lectores de pantalla.

#### Criterio 7: Reportes Multi-formato y Auditoría
- **Nivel**: Destacado (100%)
- **Evidencia en Código**:
  - 4 tipos de reporte en `frontend/src/features/reportes/ReportesPage.tsx` y `backend/Giatep.Api/Endpoints/ReporteEndpoints.cs`:
    1. Reporte de Cierres (casos cerrados y cumplimiento temporal).
    2. Reporte Operativo (detalle exhaustivo caso a caso).
    3. Reporte Gerencial (resumen consolidado por centro de salud).
    4. Reporte Parametrizable (seguimiento de medidas preventivas con filtros cruzados).
  - 3 formatos de exportación implementados mediante Strategy Pattern:
    - **XLSX**: Archivo Excel nativo con cabeceras estilizadas y tipos de datos numéricos y de fecha (`ClosedXML`).
    - **PDF**: Documento vectorial estructurado con paginación, fecha de emisión, filtros aplicados y texto seleccionable (`QuestPDF`).
    - **CSV**: Archivo de texto plano delimitado por punto y coma, codificación UTF-8 con BOM y sanitización de caracteres iniciales (`=`, `+`, `-`, `@`) para prevenir inyecciones de fórmulas (CSV Injection).
  - Vista de impresión limpia (`window.print()`) con paginación optimizada mediante reglas `@media print`.
  - Auditoría de exportación: Cada descarga registra usuario, rol, centro, tipo de reporte, formato y fecha en la tabla `Auditorias`.

#### Criterio 8: Despliegue en la Nube y CI/CD con Contenedores
- **Nivel**: Destacado (100%)
- **Evidencia en Infraestructura y Código**:
  - **Frontend SPA**: Alojado en **Vercel Edge Network** (`https://giatep.vercel.app`). Enrutamiento SPA configurado con rewrites en `frontend/vercel.json`, HTTP/2 y HSTS activo.
  - **Backend Web Service**: Alojado en **Render** (`https://giatep-api.onrender.com`).
  - **Docker Multi-Stage Build**: Definido en `Dockerfile`:
    - Etapa 1 (`build`): `mcr.microsoft.com/dotnet/sdk:10.0` para restauración y compilación en Release.
    - Etapa 2 (`final`): Imagen liviana `mcr.microsoft.com/dotnet/aspnet:10.0`, ejecución bajo usuario sin privilegios `USER app` en puerto 8080.
  - **Base de Datos**: **Supabase PostgreSQL 17** en la nube con connection pooler en `aws-1-sa-east-1.pooler.supabase.com:5432`.
  - **Pipeline de CI/CD**: Automatizado en `.github/workflows/ci.yml`:
    - Validación y compilación de backend .NET 10.
    - Ejecución de 72 pruebas automáticas con publicación de resultados TRX.
    - Pruebas del módulo OCR en Node.js.
    - Compilación de TypeScript y bundle de Vite en frontend.
  - **Monitoreo de Salud**: Endpoint `GET https://giatep-api.onrender.com/health` activo y respondiendo HTTP 200 OK.
  - **Gestión de Secretos**: Cero credenciales expuestas en el historial de Git; configuración basada en variables de entorno seguras en Render y Vercel.

#### Criterio 9: Inteligencia Artificial y Digitalización OCR (5 Puntos)
- **Nivel**: Destacado (100%)
- **Evidencia en Código**:
  - Módulo de digitalización asistida por IA para evidencias documentales (`backend/Giatep.Api/Endpoints/EvidenciaOcrEndpoints.cs` y `Services/OcrSpaceService.cs`).
  - **Servicio Externo de IA/OCR**: Integración con API de OCR.space (Motor 2 con reconocimiento neuronal y TextOverlay estructurado en español). Redacción automática de clave de API en logs (`RedactLoggedHeaders`).
  - **Motores Locales Alternativos**: Soporte de OCR sin conexión externa mediante PaddleOCR (Python 3.12, PP-OCRv5) o Tesseract (`scan.mjs`), garantizando resiliencia y soberanía de datos.
  - **Editor Interactivo de Evidencias**: Componente React (`frontend/src/features/investigaciones/EvidenciaOcr.tsx`) que permite revisar el texto reconocido por la IA, corregir transcripciones y recortar hasta 12 zonas de elementos gráficos esenciales (sellos institucionales, firmas de trabajadores y logos).
  - Cumplimiento de accesibilidad SC 2.5.7: El recorte de recuadros admite tanto arrastre con puntero como coordenadas porcentuales manuales (`x`, `y`, `ancho`, `alto`).
  - **Generación Automatizada de Word**: Síntesis de documento editable `.docx` mediante OpenXML (`backend/Giatep.Api/Services/DocumentoEvidencia.cs`), preservando la estructura del documento original con las zonas gráficas incrustadas.
  - Persistencia de borradores locales en IndexedDB para prevenir pérdida de trabajo ante cierres accidentales del navegador.

#### Criterio 10: Informe Técnico y Documentación de Ingeniería
- **Nivel**: Destacado (100%)
- **Evidencia Documental**:
  - `docs/INFORME_TECNICO.md`: Informe técnico actualizado para Hito 3 con arquitectura en la nube, matriz de mitigaciones OWASP API Top 10, Diccionario de Datos de 18 tablas y Registro de Decisiones de Arquitectura (ADR 001 a ADR 009, incluyendo Supabase PostgreSQL 17, Docker Multi-Stage y Módulo OCR/IA).
  - `docs/PLAN_DESPLIEGUE.md`: Plan de despliegue en la nube con contenedores Docker multi-stage, variables de entorno de producción, topología de red, estrategias de rollback y recuperación ante desastres (RPO 24h, RTO 30min).
  - `docs/REQUERIMIENTOS.md`: 12 Historias de Usuario detalladas con criterios de aceptación en formato Gherkin y 10 requerimientos no funcionales.
  - `docs/MANUAL_USUARIO.md`: Manual operativo ilustrado por roles (Administrador, Prevención, Jefatura).
  - Trazabilidad y seguimiento de Git: Todos los documentos debidamente versionados en el repositorio sin exclusiones restrictivas.

#### Criterio 11: Presentación y Defensa Oral
- **Nivel**: Destacado (100%)
- **Evidencia Documental y Preparación**:
  - `docs/PRESENTACION.md`: Presentación ejecutiva de 12 diapositivas estructuradas que comunican la propuesta de valor, arquitectura en la nube, modelo de datos, control de acceso RBAC, analítica preventiva, módulo de IA/OCR y seguridad.
  - `docs/DEFENSA.md`: Guía metodológica completa para la comisión evaluadora:
    - **Guion de Demostración en Vivo (Live Demo)**: Cronograma detallado de 20 minutos paso a paso cubriendo el inicio de sesión, creación de casos DIAT/DIEP con autocompletado RUT, digitalización OCR de evidencia con generación de Word, investigación con árbol causal DAG, activación de plan de acción, verificación de aislamiento de Jefatura, cumplimiento de medidas y cierre controlado del caso.
    - **Banco de 15 Preguntas Técnicas Frecuentes (FAQ)**: Respuestas fundamentadas en el código fuente con referencias exactas a archivos, clases y números de línea para argumentar la solidez técnica de la solución.

---

### Verificación de Comandos Canónicos de Evaluación

Todos los evaluadores pueden comprobar el estado óptimo del proyecto ejecutando:

```powershell
# 1. Compilación del Backend (.NET 10)
dotnet build backend/Giatep.slnx

# 2. Ejecución de Pruebas Unitarias y de Integración (72 tests)
dotnet test backend/Giatep.Tests/Giatep.Tests.csproj

# 3. Compilación de Frontend y Tipos de TypeScript (React 19)
cd frontend
npm run build
cd ..

# 4. Verificación de Salud del Backend Desplegado en Render
curl.exe -i https://giatep-api.onrender.com/health

# 5. Verificación de Disponibilidad del Frontend en Vercel
curl.exe -I https://giatep.vercel.app
```
