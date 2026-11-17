# Presentación de Defensa del Proyecto GIATEP
## Certamen 3 (Hito 3) · Sistema Integral en la Nube

---

## Diapositiva 1: Portada y Propósito de GIATEP

### GIATEP — Gestión e Investigación de Accidentes del Trabajo y Enfermedades Profesionales
**Certamen 3 (Hito 3) · Versión Final de Producción**  
*Asignatura: Lenguaje de Programación Web II*  
*Integración Curricular: Programación Orientada a Objetos II, Modelamiento de Bases de Datos, Inteligencia de Negocios I y II*

- **Propósito Central**: Transformar la gestión de prevención de riesgos laborales mediante un **Expediente Único Digital**, trazabilidad estricta de medidas correctivas y digitalización asistida por Inteligencia Artificial.
- **Arquitectura de Producción en la Nube**:
  - Frontend SPA en **Vercel Edge Network** (`https://giatep.vercel.app`).
  - Backend API en **Render Web Service** con **Docker Multi-Stage** (.NET 10).
  - Base de datos relacional de alta disponibilidad en **Supabase PostgreSQL 17**.
- **Resultado Global**: 100% de cumplimiento en nivel **Destacado** en los 11 criterios de evaluación.

---

## Diapositiva 2: El Problema Operativo y la Solución por Expediente

### De la Fragmentación al Expediente Digital Integrado

- **La Problemática en Seguridad y Salud en el Trabajo (S&SO)**:
  - Dispersión de antecedentes entre formularios físicos (DIAT/DIEP), planillas de cálculo y correos aislados.
  - Investigaciones causales incompletas o sin metodología científica formal.
  - Medidas preventivas olvidadas o sin verificación de cumplimiento efectivo en terreno.
- **La Solución GIATEP**:
  - **Expediente Único Centralizado**: Cada evento genera un registro integral inmutable que reúne antecedentes, denuncias oficiales, declaraciones testimoniales, evidencias documentales escaneadas y plan de acción.
  - **Validación Normativa**: Formularios normalizados bajo el estándar chileno de la Superintendencia de Seguridad Social (SUSESO Circular 3336), con validación matemática de RUT chileno (módulo 11) y autocompletado en tiempo real.

---

## Diapositiva 3: Control de Acceso Estricto (RBAC + Multi-Tenancy)

### Seguridad y Segregación de Responsabilidades

- **Tres Roles Claramente Delimitados**:
  1. **Administrador**: Gestión exclusiva de parámetros maestros (empleadores, centros de salud, trabajadores) y usuarios del sistema. Bloqueado de la operación de expedientes e investigaciones.
  2. **Prevención de Riesgos**: Rol transversal de gestión técnica. Registra denuncias, lidera investigaciones causales, utiliza la digitalización con IA, formula medidas y valida el cumplimiento de planes de acción.
  3. **Jefatura de Centro de Salud**: Rol operativo descentralizado. Consulta exclusivamente los casos de su propio centro y reporta la ejecución de las medidas asignadas a su cargo.
- **Aislamiento Multi-Tenant Confinado**:
  - Implementado en `AccesoService.cs`. Un usuario de Jefatura no puede acceder a ningún caso, evidencia ni reporte de otro establecimiento de salud. Cualquier intento de manipulación de IDs en la URL retorna HTTP 404 Problem Details sin filtrar información sensible.

---

## Diapositiva 4: Ciclo de Vida del Caso e Investigación Causal (DAG)

### Flujo Operativo Riguroso sin Regresiones

```
[ Borrador ]  --->  [ En investigación ]  --->  [ En seguimiento ]  --->  [ Cerrado ]
  (Denuncia)          (Causalidad & OCR)         (Plan de Acción)        (100% Verif.)
```

- **Apertura de Investigación Independiente**:
  - Al iniciar la investigación, se genera una entidad autónoma y se captura una instantánea (*snapshot*) de antecedentes para auditoría inalterable.
- **Metodología de Árbol de Causas (Método MAC)**:
  - Modelado matemático mediante un **Grafo Acíclico Dirigido (DAG)**.
  - Algoritmo de detección de ciclos en profundidad (`Alcanza`), impidiendo referencias circulares y autorrelaciones (`AntecedenteId <> ConsecuenciaId`).
  - Restricción estricta de un único Hecho Final como desenlace de la cadena causal.
- **Puerta de Cierre de Investigación**:
  - Exige el cumplimiento de 7 requisitos indispensables: entrevistas, evidencias, relato final >= 20 caracteres, hechos antecedentes, árbol conectado y medidas asignadas a hechos no finales.

---

## Diapositiva 5: Arquitectura en Capas y Patrones de Software

### Principios SOLID y Desacoplamiento en .NET 10

- **Clean Architecture**:
  - Capa de Presentación (Minimal APIs en `Endpoints/`).
  - Capa de Lógica de Negocio (`Services/CasoService.cs`, `InvestigacionService.cs`, `AccesoService.cs`, `AnalisisService.cs`).
  - Capa de Acceso a Datos (`Repositories/` y `Models/`).
- **Patrón Repository (`ICasoRepository`)**:
  - Desacopla la lógica de negocio del motor de base de datos específico (EF Core). Facilita pruebas unitarias automáticas con repositorios en memoria sin requerir SQL.
- **Patrón Strategy (`IExportador`)**:
  - Encapsula la generación de reportes multi-formato (`ExcelExportador`, `PdfExportador`, `CsvExportador`), permitiendo extender formatos de exportación sin alterar las reglas del negocio.
- **Inyección de Dependencias Nativa**:
  - Registro desacoplado de repositorios, exportadores y servicios de dominio en `Program.cs`.
- **Problem Details (RFC 7807 y RFC 9110)**:
  - Respuestas estructuradas y unificadas de error para toda la API (`title`, `detail`, `status`, `errors`).

---

## Diapositiva 6: Modelo de Datos en Supabase PostgreSQL 17

### Persistencia Relacional Robusta y Concurrente

- **Migración Exitosa a la Nube (ADR 007)**:
  - Instancia gestionada de **PostgreSQL 17** en Supabase (`aws-1-sa-east-1.pooler.supabase.com:5432`), con connection pooler transaccional (PgBouncer).
- **Esquema Normalizado de 18 Tablas Relacionales**:
  - Identificadores únicos basados en UUID nativo.
  - Manejo consistente de marcas temporales con zona horaria (`timestamp with time zone`).
- **Integridad y Restricciones a Nivel de Base de Datos**:
  - Restricciones CHECK para estados de caso, roles de usuario, tipos de denuncia y metadatos de archivos.
  - Claves foráneas con regla `ON DELETE RESTRICT`, garantizando la imposibilidad de eliminar registros con historial de auditoría o investigaciones vinculadas.
  - **Índices Optimizados**: Índice compuesto `IX_Casos_Estado_Fecha` para el dashboard, e índice único filtrado `IX_Hechos_Investigacion_EsFinal` que asegura a nivel SQL la existencia de un solo hecho final por árbol causal.

---

## Diapositiva 7: Indicadores de Gestión y Analítica Preventiva

### Inteligencia de Negocios para la Reducción de la Siniestralidad

- **6 Indicadores Clave de Desempeño (KPIs)** con fórmulas matemáticas explícitas visibles en las tarjetas:
  1. **Volumen de Casos**: Carga total de eventos en el período.
  2. **Carga Abierta**: Casos activos que requieren gestión (`Borrador`, `EnInvestigacion`, `EnSeguimiento`).
  3. **Proporción Cerrada (%)**: Eficacia resolutiva del equipo de prevención de riesgos.
  4. **Tiempo Medio de Cierre (Días)**: Promedio temporal entre la ocurrencia y el cierre definitivo del caso.
  5. **Cumplimiento de Medidas (%)**: Porcentaje de acciones correctivas implementadas y verificadas.
  6. **Atraso de Medidas (%)**: Medidas que superaron su fecha plazo sin completarse.
- **Robustez Matemática**: Tratamiento seguro de denominadores cero (retornando `0` de forma controlada sin excepciones de runtime).

---

## Diapositiva 8: Dashboard Horizon UI y Accesibilidad Universal

### Experiencia Visual Moderna y Conforme a WCAG 2.2 AA

- **Dashboard Interactivo**:
  - 4 visualizaciones interactivas: Tendencia mensual (líneas), Distribución por tipo de evento (donut), Volumen por centro de salud (barras) y Avance por estado (funnel).
  - Filtros combinados bidireccionales por fecha y establecimiento, con auto-actualización cada 60s y al recuperar foco.
- **Accesibilidad Universal (WCAG 2.2 Nivel AA)**:
  - **Diseño Horizon UI**: Ratios de contraste superiores a 7:1 (AAA) en modos claro y oscuro.
  - **Navegación Íntegra por Teclado**: Enlace directo para saltar al contenido (`skip link`) y focos visibles (`:focus-visible`).
  - **WCAG 2.2 SC 2.4.11**: Propiedad `scroll-padding-top: 90px;` en CSS para evitar que la barra superior fija (82px) tape elementos en foco.
  - **Tablas Semánticas Alternativas**: Todos los gráficos SVG cuentan con tablas HTML semánticas desplegables (`<details className="chart-table">`) para lectores de pantalla.
  - **Auditoría axe-core**: 0 violaciones en las 17 pantallas principales del sistema.

---

## Diapositiva 9: Reportes Multi-Formato y Auditoría

### Información Oportuna para Decisiones Gerenciales y Fiscalizaciones

- **4 Reportes Especializados**:
  1. **Reporte de Cierres**: Casos cerrados con detalle de plazos de cumplimiento.
  2. **Reporte Operativo**: Detalle minucioso de cada evento para auditorías internas.
  3. **Reporte Gerencial**: Resumen estadístico agrupado por centro de salud para comités paritarios y directivos.
  4. **Reporte Parametrizable**: Seguimiento granular de medidas preventivas con filtros cruzados.
- **Formatos de Exportación Auditados**:
  - **Excel (XLSX)**: Formateo numérico real y estilos institucionales.
  - **PDF Vectorial**: Membrete, paginación "Página X de Y", filtros aplicados y texto seleccionable.
  - **CSV Protegido**: UTF-8 con BOM y sanitización contra inyección de fórmulas de hoja de cálculo.
  - **Impresión Optimizada**: Estilos `@media print` que suprimen menús y controles.
  - **Auditoría Transaccional**: Cada exportación registra usuario, rol, centro, formato y fecha en la tabla `Auditorias`.

---

## Diapositiva 10: Inteligencia Artificial y Digitalización OCR (5 Puntos)

### Reconocimiento Óptico y Generación Inteligente de Documentos

- **Digitalización Neuronal de Evidencias Físicas (ADR 009)**:
  - Integración externa con **OCR.space API** (Motor 2 neuronal con TextOverlay estructurado en español).
  - Motores locales alternativos de respaldo (**PaddleOCR** en Python 3.12 y **Tesseract** en Node.js) para operación offline o entornos aislados.
- **Editor Interactivo de Transcripción**:
  - Superposición visual de cajas de texto sobre la imagen escaneada para corrección humana en tiempo real.
  - **Recorte Gráfico Asistido**: Selección precisa de hasta 12 zonas gráficas para preservar firmas de trabajadores, sellos institucionales y timbres de mutualidades.
  - Cumplimiento de **WCAG 2.2 SC 2.5.7**: Controles manuales numéricos de coordenadas (`x`, `y`, `ancho`, `alto`) además del puntero del mouse.
- **Síntesis Automatizada de Word (DOCX)**:
  - Generación de documentos editables mediante **OpenXML**, reproduciendo la distribución espacial original e incrustando las firmas recortadas.

---

## Diapositiva 11: Despliegue en la Nube con Docker Multi-Stage

### Infraestructura Escalable, Segura y de Alta Disponibilidad

- **Docker Multi-Stage Build (ADR 008)**:
  - Etapa de compilación con SDK .NET 10 y etapa final liviana con ASP.NET Core 10 Runtime.
  - Ejecución bajo el usuario seguro sin privilegios `USER app` en puerto 8080.
- **Topología Distribuida**:
  - **Frontend**: Desplegado en **Vercel** (`https://giatep.vercel.app`) con Edge Routing y HSTS.
  - **Backend**: Desplegado en **Render** (`https://giatep-api.onrender.com`) conectado a Supabase.
  - **Base de Datos**: **Supabase PostgreSQL 17** con pooling y respaldos continuos.
- **Monitoreo Continuo**:
  - Endpoint `GET https://giatep-api.onrender.com/health` retornando HTTP 200 OK y estado disponible.
  - Pipeline de CI/CD automatizado en GitHub Actions ejecutando compilación y 72 pruebas automáticas en cada commit.

---

## Diapositiva 12: Seguridad (OWASP API Top 10) y Cumplimiento 100%

### Solidez Técnica Comprobada y Conclusiones

- **Mitigación Exhaustiva OWASP API Security Top 10**:
  - Prevención de BOLA mediante validación estricta por establecimiento en `AccesoService`.
  - Hashing PBKDF2 y revocación inmediata de sesiones activas mediante claim `version` en `OnTokenValidated`.
  - 4 limitadores de tasa (Rate Limiting), cabeceras de seguridad estrictas (`nosniff`, `DENY`, `no-store`) y redacción de claves de API en logs.
- **Suite de Pruebas Automáticas**:
  - 72 pruebas automáticas en .NET 10 (`backend/Giatep.Tests`) aprobadas al 100% (0 fallos).
- **Cumplimiento de la Rúbrica de Certamen 3**:
  - **11 de 11 criterios cumplidos en nivel Destacado (100%)**, combinando excelencia arquitectónica, diseño accesible, base de datos relacional avanzada, despliegue en la nube e innovación con Inteligencia Artificial.
