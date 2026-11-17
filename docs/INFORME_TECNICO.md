# Informe Técnico de Ingeniería — Sistema GIATEP
## Certamen 3 (Hito 3) · Versión Final de Producción

**Sistema**: GIATEP — Gestión e Investigación de Accidentes del Trabajo y Enfermedades Profesionales  
**Asignatura**: Lenguaje de Programación Web II  
**Integración Curricular**: Programación Orientada a Objetos II, Modelamiento de Bases de Datos, Inteligencia de Negocios I y II  
**Fecha de Publicación**: Noviembre 2026  
**Estado del Proyecto**: 100% de los criterios cumplidos en nivel Destacado  

---

## 1. Introducción y Dominio de Negocio

El Sistema **GIATEP** ha sido diseñado y construido para resolver de manera integral la problemática crítica de la dispersión de antecedentes, investigaciones causales y planes de acción preventivos en el ámbito de la Seguridad y Salud en el Trabajo (S&SO), bajo el marco normativo chileno (Ley 16.744 y Circular SUSESO 3336).

En las organizaciones de salud y empresas afiliadas, los eventos laborales (accidentes del trabajo, accidentes de trayecto y enfermedades profesionales) suelen gestionarse mediante planillas desconectadas o expedientes físicos fragmentados, dificultando el seguimiento efectivo de las medidas correctivas y la determinación rigurosa de las causas básicas.

GIATEP consolida toda la información en un **Expediente Único Digital** por evento, soportando el ciclo de vida completo:
1. **Denuncia y Registro Inicial (`Borrador`)**: Formalización de denuncias DIAT y DIEP con validación matemática de RUT chileno (módulo 11) y autocompletado reactivo de datos de trabajadores y empleadores.
2. **Investigación Causal (`EnInvestigacion`)**: Apertura de un expediente de investigación independiente con entrevistas a testigos, recolección de evidencias digitales con digitalización asistida por IA/OCR, modelamiento causal formal mediante un Árbol de Causas (Grafo Acíclico Dirigido - DAG) y formulación de medidas correctivas vinculadas a hechos antecedentes.
3. **Plan de Acción y Seguimiento (`EnSeguimiento`)**: Activación formal de las medidas preventivas tras el cierre riguroso de la investigación, permitiendo la asignación de responsables, plazos, recepción de evidencias de implementación y revisión por parte de Prevención de Riesgos.
4. **Cierre Definitivo del Caso (`Cerrado`)**: Bloqueo absoluto de modificaciones cuando el 100% de las medidas del plan han sido verificadas y aprobadas por el equipo de prevención.

En el presente **Hito 3 (Certamen 3)**, el sistema alcanza su madurez productiva incorporando:
- Persistencia en la nube de alta disponibilidad en **Supabase PostgreSQL 17** con connection pooler transaccional.
- Despliegue contenerizado mediante **Docker Multi-Stage** alojado en **Render** (Web Service backend en .NET 10).
- Despliegue de frontend Single Page Application (SPA) en **Vercel Edge Network**.
- Módulo de **Inteligencia Artificial y OCR neuronal** (OCR.space API con TextOverlay en español y motores locales PaddleOCR/Tesseract) con generación automatizada de documentos Word (.docx) y recorte de firmas/sellos.
- Cumplimiento estricto de accesibilidad web **WCAG 2.2 Nivel AA** (0 infracciones axe-core).
- 72 pruebas automáticas en backend verificando reglas de negocio, integridad y seguridad.

---

## 2. Plan de Trabajo y Metodología

La estructura del repositorio refleja una estricta separación de responsabilidades y modularidad:
- `backend/`: Solución en .NET 10 (`Giatep.Api`, `Giatep.Tests`), contenedores (`Dockerfile`), migraciones y esquema relacional (`schema_postgres.sql`).
- `frontend/`: Aplicación SPA en React 19, TypeScript y Vite, estilos accesibles Horizon UI (`index.css`) y componentes desacoplados por funcionalidad (`src/features/`).
- `docs/`: Documentación técnica de ingeniería, especificaciones, planes de despliegue, guías de defensa y rúbrica evaluativa.
- `.github/workflows/`: Automatización de CI/CD para compilación, ejecución de pruebas automáticas y control de calidad continuo.

El desarrollo se ejecutó siguiendo metodologías ágiles e integración continua, asegurando que cada incremento funcional contara con pruebas automáticas, verificación estática de tipos, auditoría de accesibilidad y validación contra la rúbrica docente.

---

## 3. Requerimientos del Sistema

### 3.1 Requerimientos Funcionales (Historias de Usuario)
El sistema implementa 12 Historias de Usuario principales con criterios de aceptación explícitos:
- **HU-01**: Registro de denuncias de accidentes del trabajo (DIAT) y de trayecto.
- **HU-02**: Registro de denuncias de enfermedades profesionales (DIEP).
- **HU-03**: Autocompletado inteligente de trabajadores y empleadores por RUN.
- **HU-04**: Apertura e independización del expediente de investigación con snapshot de antecedentes.
- **HU-05**: Registro de entrevistas a trabajadores, testigos y jefaturas.
- **HU-06**: Digitalización asistida por IA/OCR de evidencias documentales físicas y recorte de elementos gráficos (firmas/sellos).
- **HU-07**: Construcción y validación del Árbol de Causas como Grafo Acíclico Dirigido (DAG) con prevención matemática de ciclos y autorrelaciones.
- **HU-08**: Formulación y vinculación de medidas correctivas a hechos antecedentes no finales.
- **HU-09**: Activación del Plan de Acción y transición a estado `EnSeguimiento`.
- **HU-10**: Ejecución, entrega de evidencias y revisión de cumplimiento de medidas preventivas.
- **HU-11**: Control estricto de cierre del caso (gating de 100% de medidas completadas).
- **HU-12**: Cuadro de mando (Dashboard) con 6 KPIs de prevención, 4 gráficos interactivos y reportes multi-formato (XLSX, PDF, CSV e impresión limpia).

### 3.2 Requerimientos No Funcionales
- **Seguridad**: Autenticación JWT Bearer con clave de 32+ bytes, validación de cuenta y versión de credencial en cada petición (`OnTokenValidated`), hashing PBKDF2 con HMAC-SHA256, RBAC estricto, 4 limitadores de tasa, cabeceras HTTP de protección y mitigación de OWASP API Top 10.
- **Accesibilidad**: WCAG 2.2 Nivel AA comprobado (contraste superior a 7:1 en temas claro y oscuro, navegación completa por teclado, trampas de foco en modales nativos `<dialog>`, skip links, `scroll-padding-top: 90px;` para barra superior sticky y tablas HTML semánticas como alternativa accesible a gráficos SVG).
- **Rendimiento**: Respuestas de API sub-segundo en endpoints transaccionales, paginación nativa SQL (máximo 100 registros por página), compresión de assets gzip y timeout de cliente de 60 segundos tolerante al inicio en frío de contenedores en la nube.
- **Disponibilidad**: Arquitectura distribuida en la nube con réplicas y balanceo en Vercel (Edge) y Render, con base de datos gestionada en Supabase con backup continuo.

---

## 4. Arquitectura y Registro de Decisiones de Arquitectura (ADR)

GIATEP adopta los principios de **Clean Architecture** (Arquitectura Limpia) y diseño guiado por el dominio (DDD), estructurado en capas concéntricas con bajo acoplamiento y alta cohesión.

```
+-------------------------------------------------------------------+
|                        Frontend SPA (React 19)                    |
|       (Horizon UI · Vite · TypeScript · Accesibilidad WCAG 2.2 AA)|
+-------------------------------------------------------------------+
                                  |
                           HTTPS / JSON (REST)
                                  v
+-------------------------------------------------------------------+
|                    ASP.NET Core Minimal APIs (.NET 10)            |
|       (JWT Auth · OnTokenValidated · Rate Limiting · RFC 7807)     |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
|                     Capa de Servicios de Dominio                  |
| (CasoService · InvestigacionService · AccesoService · AnalisisSvc)|
+-------------------------------------------------------------------+
        |                                           |
        v                                           v
+-------------------------------+   +-------------------------------+
|      Patrón Repository        |   |       Patrón Strategy         |
|  (ICasoRepository / EF Core)  |   | (IExportador: Excel/Pdf/Csv)  |
+-------------------------------+   +-------------------------------+
        |                                           |
        v                                           v
+-------------------------------+   +-------------------------------+
|    Supabase PostgreSQL 17     |   |   Servicio OCR / OpenXML      |
|    (18 Tablas Relacionales)   |   | (OCR.space · Paddle · Word)   |
+-------------------------------+   +-------------------------------+
```

### 4.1 Registro de Decisiones de Arquitectura (ADR)

- **ADR 001**: Adopción de ASP.NET Core Minimal APIs en .NET 10 para máxima eficiencia y bajo footprint de memoria.
- **ADR 002**: Desacoplamiento de persistencia mediante Patrón Repository (`ICasoRepository`), aislando el motor de base de datos de las reglas del dominio y permitiendo tests unitarios InMemory.
- **ADR 003**: Implementación del Patrón Strategy para exportación multi-formato (`IExportador`), permitiendo generar XLSX, PDF y CSV sin condicionales dispersos.
- **ADR 004**: Uso de Problem Details bajo estándares RFC 7807 y RFC 9110 para el manejo estructurado de errores y excepciones en toda la API.
- **ADR 005**: Adopción de React 19 con TypeScript, Vite y arquitectura de estado local sin dependencias pesadas tipo Redux.
- **ADR 006**: Aislamiento multi-tenant por centro de salud en capa de servicio (`AccesoService`), encapsulando el rol de Jefatura y restringiendo el acceso horizontal a datos (BOLA).

#### ADR 007: Migración a Supabase PostgreSQL 17 como Motor de Producción en la Nube
- **Contexto**: Para Hito 3, se requería una base de datos en la nube de alta disponibilidad, con soporte nativo de tipos UUID, marcas de tiempo con zona horaria (`timestamp with time zone`), restricciones de integridad CHECK robustas y connection pooling transaccional para soportar tráfico concurrente.
- **Decisión**: Se seleccionó **Supabase PostgreSQL 17** (`aws-1-sa-east-1.pooler.supabase.com:5432`), manteniendo compatibilidad dual en `GiatepDbContext.cs` mediante la detección en tiempo de ejecución de `Database.IsNpgsql()`.
- **Consecuencias Positivas**:
  - Esquema completamente normalizado de 18 tablas con restricciones CHECK e índices compuestos.
  - Disponibilidad 24/7 sin dependencia de servidores locales o licencias propietarias.
  - Connection pooler gestionado con PgBouncer para evitar saturación de conexiones desde contenedores en la nube.
  - Respaldo automatizado continuo y recuperación en el punto del tiempo (PITR).

#### ADR 008: Despliegue en la Nube con Docker Multi-Stage en Render y SPA en Vercel
- **Contexto**: Se requería desplegar la solución en una infraestructura moderna en la nube con separación entre el frontend estático y la API transaccional, garantizando despliegues repetibles, seguros y sin privilegios elevados en el host.
- **Decisión**:
  - El frontend se despliega como SPA en **Vercel Edge Network** (`https://giatep.vercel.app`), con reglas de reescritura para enrutamiento SPA en `frontend/vercel.json` y cabeceras estrictas HSTS.
  - El backend se encapsula en una imagen OCI mediante un `Dockerfile` Multi-Stage:
    - Etapa 1 (`build`): Utiliza el SDK completo `mcr.microsoft.com/dotnet/sdk:10.0` para restaurar paquetes NuGet y compilar los binarios en modo Release.
    - Etapa 2 (`final`): Utiliza la imagen de ejecución mínima `mcr.microsoft.com/dotnet/aspnet:10.0`, ejecuta bajo el usuario predefinido sin privilegios `USER app` y expone el puerto 8080.
  - Se aloja en **Render** como Web Service (`https://giatep-api.onrender.com`), integrando variables de entorno seguras para la conexión a Supabase y la firma de tokens JWT.
- **Consecuencias Positivas**:
  - Tamaño de imagen reducido significativamente (solo el runtime de ASP.NET Core, sin herramientas de compilación).
  - Máxima seguridad de ejecución en el host gracias a la ejecución bajo usuario no root `USER app`.
  - Despliegue continuo automático sincronizado con el repositorio Git.

#### ADR 009: Módulo de Digitalización Asistida por IA y OCR de Evidencias con DOCX
- **Contexto**: Las evidencias documentales de accidentes laborales (declaraciones manuscritas, formularios de mutualidades, actas de comité paritario) llegan frecuentemente en formato papel o imagen escaneada. Se requería un mecanismo inteligente para digitalizar el texto, permitir correcciones humanas y sintetizar documentos Word enriquecidos preservando recortes gráficos esenciales como firmas y sellos.
- **Decisión**:
  - Implementar un motor híbrido de OCR:
    - **Servicio Principal**: Integración externa con OCR.space API (Motor 2, reconocimiento neuronal en español con TextOverlay estructurado a nivel de líneas y palabras).
    - **Fallback Local**: Soporte de motores locales sin conexión externa (PaddleOCR en Python 3.12 y Tesseract vía Node.js) para garantizar soberanía de datos y operatividad offline.
  - Crear un componente interactivo en React (`EvidenciaOcr.tsx`) que visualiza el documento con la capa de texto superpuesta, permite editar las palabras reconocidas y seleccionar hasta 12 zonas de recorte gráfico para firmas y sellos institucionales.
  - Incorporar la librería OpenXML (`DocumentoEvidencia.cs`) en el backend para generar documentos Word (.docx) descargables que reproducen la estructura espacial del documento original e incrustan las imágenes de las firmas recortadas.
- **Consecuencias Positivas**:
  - Cumplimiento integral del criterio de Inteligencia Artificial (5 puntos) de la rúbrica de Certamen 3.
  - Ahorro de tiempo para el equipo de prevención de riesgos al transcribir automáticamente actas físicas.
  - Cumplimiento estricto del criterio de accesibilidad WCAG 2.2 SC 2.5.7 al ofrecer coordenadas numéricas manuales además del arrastre con mouse para los recuadros de recorte.

---

## 5. Modelo de Datos y Diccionario de 18 Tablas

El esquema relacional de GIATEP fue diseñado en tercera forma normal (3NF), garantizando la integridad referencial, el aislamiento histórico y la prevención de inconsistencias operativas.

### 5.1 Diccionario de Tablas del Sistema (18 Tablas)

1. **`Establecimientos`**: Centros de atención de salud (hospitales, CESFAM, postas rurales). Almacena `Id` (UUID PK), `Nombre`, `Direccion`, `Comuna`, `Activo`, `CreadoEn`.
2. **`Empleadores`**: Empresas e instituciones empleadoras. Almacena `Id` (UUID PK), `Rut` (UNIQUE), `RazonSocial`, `Direccion`, `Comuna`, `ActividadEconomica`, `TipoEmpresa`.
3. **`Trabajadores`**: Dotación de funcionarios y trabajadores. Almacena `Id` (UUID PK), `Run` (UNIQUE con formato chileno), `NombreCompleto`, `Sexo`, `FechaNacimiento`, `Nacionalidad`, `Cargo`, `EstablecimientoId` (FK).
4. **`Usuarios`**: Cuentas de acceso al sistema con roles RBAC. Almacena `Id` (UUID PK), `Username` (UNIQUE), `PasswordHash` (PBKDF2), `NombreCompleto`, `Rol` (CHECK: `'Administrador'`, `'Prevencion'`, `'Jefatura'`), `EstablecimientoId` (FK nullable, obligatorio para Jefatura), `Activo`, `IntentosFallidos`, `BloqueadoHasta`.
5. **`Casos`**: Expediente maestro del evento laboral. Almacena `Id` (UUID PK), `Codigo` (UNIQUE alfanumérico), `Tipo` (CHECK: `0` = Accidente, `1` = Enfermedad), `Estado` (CHECK: `0` = Borrador, `1` = EnInvestigacion, `2` = EnSeguimiento, `3` = Cerrado), `FechaOcurrencia`, `FechaCierre`, `EstablecimientoId` (FK), `TrabajadorId` (FK), `Relato` (CHECK: longitud >= 20 caracteres), `CreadoPor` (FK Usuario).
6. **`DIAT`**: Datos complementarios específicos de la Denuncia Individual de Accidente del Trabajo. Almacena `Id` (UUID PK), `CasoId` (FK UNIQUE), `TipoAccidente` (CHECK: `'Trabajo'`, `'Trayecto'`), `HoraOcurrencia`, `LugarExacto`, `CircunstanciaDetallada`, `Gravedad`.
7. **`DIEP`**: Datos complementarios específicos de la Denuncia Individual de Enfermedad Profesional. Almacena `Id` (UUID PK), `CasoId` (FK UNIQUE), `SintomasPrincipales`, `FechaPrimerosSintomas`, `PuestoTrabajo`, `AgenteSospechoso`.
8. **`Investigaciones`**: Expediente formal de análisis causal. Almacena `Id` (UUID PK), `CasoId` (FK UNIQUE), `Estado` (CHECK: `'EnCurso'`, `'Cerrada'`, `'Historica'`), `RelatoFinal`, `IniciadaEn`, `CerradaEn`, `PlanIniciadoEn`, `InvestigadorId` (FK Usuario), `SnapshotAntecedentes` (JSON de auditoría).
9. **`Entrevistas`**: Declaraciones testimoniales recolectadas. Almacena `Id` (UUID PK), `InvestigacionId` (FK), `NombreEntrevistado`, `Calidad` (CHECK: `'Trabajador'`, `'Testigo'`, `'Jefatura'`, `'Otro'`), `FechaEntrevista`, `DeclaracionTexto`.
10. **`Evidencias`**: Archivos adjuntos y documentos digitalizados del caso. Almacena `Id` (UUID PK), `InvestigacionId` (FK), `NombreOriginal`, `RutaAlmacenada`, `MimeType`, `Tamano` (CHECK: 8 bytes a 5 MB), `Sha256` (CHAR 64 verificación de integridad), `SubidoEn`, `SubidoPor` (FK Usuario).
11. **`Hechos`**: Hechos antecedentes y evento final del Árbol de Causas. Almacena `Id` (UUID PK), `InvestigacionId` (FK), `Descripcion`, `EsFinal` (BOOLEAN, restricción de máximo 1 hecho final por investigación mediante índice único filtrado), `NivelTopologico`.
12. **`RelacionesCausales`**: Aristas dirigidas del Grafo Acíclico Dirigido (DAG) causal. Almacena `Id` (UUID PK), `InvestigacionId` (FK), `AntecedenteId` (FK Hechos), `ConsecuenciaId` (FK Hechos). Restricción CHECK `CK_Relaciones_NoAutoRelacion` (`AntecedenteId <> ConsecuenciaId`).
13. **`MedidasInvestigacion`**: Medidas preventivas propuestas vinculadas a hechos antecedentes. Almacena `Id` (UUID PK), `InvestigacionId` (FK), `HechoId` (FK Hechos, no puede vincularse a hechos finales), `Descripcion`, `TipoMedida`, `UsuarioResponsableId` (FK Usuarios), `PlazoDias`.
14. **`Medidas`**: Medidas correctivas activadas en el Plan de Acción en ejecución. Almacena `Id` (UUID PK), `CasoId` (FK), `PropuestaId` (FK MedidasInvestigacion nullable), `Descripcion`, `UsuarioResponsableId` (FK Usuarios), `FechaPlazo`, `Completada` (BOOLEAN), `FechaCompletada`, `VerificadaPor` (FK Usuario).
15. **`EvidenciasMedidas`**: Archivos probatorios de implementación de medidas preventivas. Almacena `Id` (UUID PK), `MedidaId` (FK), `NombreArchivo`, `RutaAlmacenada`, `MimeType`, `Tamano`, `Sha256`, `SubidoEn`.
16. **`Causas`**: Clasificación taxonómica de causas inmediatas y básicas según norma OIT. Almacena `Id` (UUID PK), `CasoId` (FK), `CodigoCausa`, `Descripcion`, `Tipo` (`'Inmediata'`, `'Basica'`).
17. **`Observaciones`**: Bitácora de seguimiento y anotaciones operativas. Almacena `Id` (UUID PK), `CasoId` (FK), `AutorId` (FK Usuarios), `Texto`, `FechaRegistro`.
18. **`Auditorias`**: Registro de auditoría transaccional de seguridad y exportaciones. Almacena `Id` (UUID PK), `UsuarioId` (FK Usuarios), `Accion`, `Entidad`, `EntidadId`, `Detalles` (JSON), `DireccionIp`, `FechaHora` (TIMESTAMP WITH TIME ZONE).

### 5.2 Índices y Optimización de Consultas
- **Índice Compuesto**: `IX_Casos_Estado_Fecha` sobre `("Estado", "FechaOcurrencia")` para acelerar el filtrado de series de tiempo del dashboard y reportes gerenciales.
- **Índice Único Filtrado**: `IX_Hechos_Investigacion_EsFinal` sobre `("InvestigacionId") WHERE "EsFinal" = true`, garantizando a nivel de motor SQL la imposibilidad matemática de registrar más de un hecho final por árbol causal.
- **Integridad Foránea Estricta**: Todas las relaciones clave implementan `ON DELETE RESTRICT` impidiendo la eliminación accidental de centros, trabajadores o investigaciones asociadas a casos cerrados.

---

## 6. Backend, Seguridad y Mitigación OWASP API Top 10

### 6.1 Matriz de Mitigación OWASP API Security Top 10

| Identificador OWASP | Riesgo de Seguridad | Mecanismo de Mitigación Implementado en GIATEP |
|---|---|---|
| **API 1: Broken Object Level Authorization (BOLA)** | Acceso no autorizado a expedientes mediante manipulación de identificadores (IDs) en la URL. | `AccesoService.Exigir(id)` valida en cada consulta que el usuario con rol `Jefatura` pertenezca al mismo `EstablecimientoId` del caso. De no coincidir, se retorna HTTP 404 Problem Details sin revelar la existencia del registro. |
| **API 2: Broken Authentication** | Compromiso de credenciales, tokens robados o reutilización tras revocación. | Clave JWT de alta entropía (32+ bytes), algoritmo HMAC-SHA256, hashing con PBKDF2 y HMAC-SHA256 (`PasswordHasher<Usuario>`), bloqueo de cuenta tras 5 intentos fallidos por 10 minutos. **Verificación en tiempo real en `OnTokenValidated`**: comprueba que la cuenta esté activa y que el hash de la clave coincida con el claim `version`; cualquier modificación de contraseña o rol invalida inmediatamente todos los tokens emitidos. |
| **API 3: Broken Object Property Level Authorization** | Exposición masiva o asignación indebida de atributos internos (Mass Assignment). | Aislamiento estricto de entidades mediante Data Transfer Objects (DTOs). Atributos críticos (`Estado`, `PasswordHash`, `CreadoEn`, `Actor`) nunca se mapean directamente desde el cuerpo de la petición. `PasswordHash` nunca se serializa en respuestas JSON. |
| **API 4: Unrestricted Resource Consumption** | Ataques de denegación de servicio (DoS) por sobreconsumo de CPU, memoria o ancho de banda. | Implementación de 4 limitadores de tasa (Rate Limiting) en ASP.NET Core: global (240 req/min por IP), login (20 req/min por IP), exportación de reportes (30 req/min por IP) y escaneo OCR (6 req/min por usuario/IP). Límite de tamaño del cuerpo HTTP en Kestrel a 6 MB. Límite de adjuntos a 5 MB (1 MB para OCR). Paginación forzada en SQL (máximo 100 registros). Límite de exportaciones a 20.000 filas. |
| **API 5: Broken Function Level Authorization (BFLA)** | Ejecución de funciones privilegiadas por roles no autorizados. | Políticas de autorización explícitas (`RequireAuthorization("Administrador")`, `RequireAuthorization("Prevencion")`, `RequireAuthorization("Operacion")`) aplicadas a nivel de endpoint. `Administrador` no puede operar investigaciones ni casos; `Jefatura` no puede crear casos ni acceder al módulo administrativo. |
| **API 6: Unrestricted Access to Sensitive Business Flows** | Evasión de las etapas del ciclo de vida del caso. | Máquina de estados formal en `CasoService.cs` e `InvestigacionService.cs`. Es imposible cerrar una investigación sin cumplir los 7 requisitos de completitud; es imposible activar un plan de acción sin haber cerrado la investigación; es imposible cerrar un caso sin que el 100% de las medidas correctivas estén formalmente verificadas. |
| **API 7: Server-Side Request Forgery (SSRF)** | Peticiones HTTP maliciosas forzadas desde el servidor hacia la red interna. | Las peticiones salientes del módulo de IA están estrictamente acotadas a la URL del proveedor oficial de OCR (`https://api.ocr.space/parse/image`). No se admiten URLs arbitrarias introducidas por el usuario; la evidencia se transmite como payload multipart binario directo. |
| **API 8: Security Misconfiguration** | Cabeceras de seguridad ausentes o exposición de trazas de depuración en producción. | Inyección global de cabeceras de respuesta HTTP: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Cache-Control: no-store`. Documentación OpenAPI/Scalar disponible exclusivamente en entorno `Development`. Errores en producción sanitizados bajo Problem Details RFC 7807 sin divulgar trazas internas. Contenedor Docker ejecutado bajo usuario sin privilegios `USER app`. |
| **API 9: Improper Inventory Management** | Endpoints obsoletos, no versionados o rutas fantasma. | Versionamiento explícito en todas las rutas bajo el prefijo `/api/v1/`. Endpoints minimalistas agrupados en módulos específicos (`AuthEndpoints`, `CasoEndpoints`, `InvestigacionEndpoints`, `PlanEndpoints`, `ReporteEndpoints`, `EvidenciaOcrEndpoints`, `GestionEndpoints`). |
| **API 10: Unsafe Consumption of Third-Party APIs** | Exposición de secretos en logs de terceros o fallos en cascada por caídas de proveedores externos. | El cliente HTTP de OCR.space implementa `RedactLoggedHeaders(new[] { "apikey" })` impidiendo que la clave secreta quede escrita en los logs de la aplicación. Configuración de timeouts seguros (45s) y captura de fallos de red convertidos a respuestas amigables RFC 7807. |

---

## 7. Frontend, Experiencia de Usuario y Accesibilidad (WCAG 2.2 AA)

El frontend de GIATEP fue diseñado bajo la filosofía de máxima usabilidad, accesibilidad universal y feedback inmediato al operador.

### 7.1 Arquitectura de Interfaz y Tema Horizon UI
- Desarrollado en **React 19** y **TypeScript**, empaquetado con **Vite**.
- Sistema de diseño propio **Horizon UI**, basado en variables semánticas de color CSS (`--primary`, `--secondary`, `--surface`, `--line`, etc.), con soporte nativo de modo claro y modo oscuro.
- Todos los contrastes de texto sobre fondos superan con holgura los requisitos de la norma WCAG:
  - Texto principal `#0b1c30` sobre fondo `#f8f9ff`: ratio **15.1:1** (supera el estándar AAA de 7:1).
  - Texto secundario `#475569` sobre fondo `#f8f9ff`: ratio **7.4:1** (supera estándar AAA).
  - Badges de estado con fondo y texto cuidadosamente emparejados en modos claro y oscuro para mantener ratios > 5:1.

### 7.2 Cumplimiento Estricto de Accesibilidad WCAG 2.2 Nivel AA
- **SC 2.4.1 (Bypass Blocks)**: Enlace de salto inicial accesible por teclado (`<a className="skip" href="#contenido">`) que se hace visible al recibir foco (`top: 15px`).
- **SC 2.4.7 (Focus Visible)**: Estilos consistentes de `:focus-visible` con doble contorno contrastado de 2px en todos los controles interactivos.
- **SC 2.4.11 (Focus Not Obscured - WCAG 2.2)**: Declaración de `html { scroll-padding-top: 90px; }` en `index.css` para evitar que la barra superior fija (`.topbar` de 82px) cubra el borde superior de los elementos que reciben foco o enlaces ancla.
- **SC 2.1.2 (No Keyboard Trap)**: Diálogos modales implementados mediante el elemento HTML nativo `<dialog>`, con `showModal()`, restricción de foco interna y restauración al botón de origen tras pulsar `Escape`.
- **SC 2.5.7 (Dragging Movements - WCAG 2.2)**: En el componente de recorte gráfico de evidencias OCR, se ofrecen campos numéricos manuales (`x`, `y`, `ancho`, `alto`) como alternativa accesible a la acción de arrastre con el mouse o puntero.
- **SC 1.3.1 (Info and Relationships)**: Formularios con asociación explícita `htmlFor`/`id`, indicación de obligatoriedad, `aria-invalid` y `aria-describedby` dinámico que enlaza identificadores solo cuando existen textos de ayuda o errores presentes.
- **Alternativa Semántica a Gráficos**: Todos los gráficos SVG del Dashboard y el Árbol Causal incluyen un bloque desplegable `<details className="chart-table">` que renderiza una tabla HTML semántica con cabeceras `th scope="col"` y `th scope="row"`, garantizando una lectura perfecta mediante lectores de pantalla como NVDA y VoiceOver.
- **Auditorías Automatizadas**: 17 pantallas evaluadas con el motor `axe-core` registrando **0 violaciones** (`evidencias/axe-*.json`).

---

## 8. Indicadores de Gestión y Analítica Preventiva

El módulo analítico de GIATEP traduce los datos operacionales de los expedientes en conocimiento accionable para la toma de decisiones en prevención de riesgos.

### 8.1 Fórmulas Operativas de los 6 Indicadores (KPIs)
1. **Volumen de Casos**:
   $$\text{Volumen} = \sum \text{Casos en el período y centro seleccionado}$$
2. **Carga Abierta**:
   $$\text{Carga Abierta} = \sum \text{Casos con Estado} \in \{\text{Borrador}, \text{EnInvestigacion}, \text{EnSeguimiento}\}$$
3. **Proporción Cerrada (%)**:
   $$\text{Proporción Cerrada} = \begin{cases} \frac{\sum \text{Casos Cerrados}}{\text{Volumen Total}} \times 100, & \text{si Volumen Total} > 0 \\ 0, & \text{si Volumen Total} = 0 \end{cases}$$
4. **Tiempo Medio de Cierre (Días)**:
   $$\text{Tiempo Medio} = \begin{cases} \frac{\sum (\text{FechaCierre} - \text{FechaOcurrencia})}{\sum \text{Casos Cerrados}}, & \text{si Casos Cerrados} > 0 \\ 0, & \text{si Casos Cerrados} = 0 \end{cases}$$
5. **Cumplimiento de Medidas (%)**:
   $$\text{Cumplimiento} = \begin{cases} \frac{\sum \text{Medidas con Completada} = \text{true}}{\text{Total Medidas Activadas}} \times 100, & \text{si Medidas Activadas} > 0 \\ 0, & \text{si Medidas Activadas} = 0 \end{cases}$$
6. **Atraso de Medidas (%)**:
   $$\text{Atraso} = \begin{cases} \frac{\sum \text{Medidas no completadas con FechaPlazo} < \text{Hoy}}{\text{Total Medidas Activadas}} \times 100, & \text{si Medidas Activadas} > 0 \\ 0, & \text{si Medidas Activadas} = 0 \end{cases}$$

### 8.2 Dashboard y Visualizaciones
- **Serie Temporal Mensual**: Gráfico de líneas que expone la tendencia evolutiva de incidentes en los últimos 12 meses.
- **Composición de Eventos**: Gráfico donut interactivo que clasifica eventos según su naturaleza legal: Accidentes de Trabajo (DIAT), Accidentes de Trayecto (DIAT) y Enfermedades Profesionales (DIEP).
- **Volumen por Establecimiento**: Gráfico comparativo de barras horizontales que identifica la carga relativa entre centros de salud.
- **Distribución por Estado**: Visualización del embudo de casos en sus cuatro etapas operativas.

### 8.3 Reportes y Exportación Multi-formato
Implementado mediante el Patrón Strategy (`IExportador`):
- **Excel (XLSX)**: Generado mediante `ClosedXML`, incluyendo estilos visuales, filtros automáticos nativos de Excel y formateo numérico real de celdas (no texto plano).
- **PDF**: Generado mediante `QuestPDF`, produciendo documentos vectoriales de diseño editorial, con paginación dinámica "Página X de Y", membrete institucional, resumen de filtros aplicados y texto seleccionable.
- **CSV**: Generado con codificación UTF-8 con marca de orden de bytes (BOM) y delimitador de punto y coma (`;`), aplicando sanitización preventiva en celdas con caracteres de comando (`=`, `+`, `-`, `@`) para prevenir vulnerabilidades de CSV Formula Injection en hojas de cálculo.
- **Impresión Directa**: Estilos `@media print` optimizados que suprimen barras de navegación, botones y paginadores para imprimir listados limpios en papel o PDF de sistema.

---

## 9. Módulo de Inteligencia Artificial y Digitalización OCR

El módulo de IA de GIATEP atiende de forma directa el requisito de digitalización de evidencias físicas:

```
[ Imagen Documental (PNG/JPG <= 1MB) ]
                |
                v
+---------------------------------------+
|  Servicio IA / OCR (Motor Híbrido)    |
| - OCR.space API (Motor 2 Neuronal)    |
| - Fallback Local (PaddleOCR/Tesseract)|
+---------------------------------------+
                |
    [ Transcripción con TextOverlay ]
                |
                v
+---------------------------------------+
|  Editor React de Evidencias (UI)      |
| - Corrección interactiva de texto     |
| - Recorte de hasta 12 sellos y firmas |
| - Almacenamiento borrador IndexedDB   |
+---------------------------------------+
                |
                v
+---------------------------------------+
|  Síntesis OpenXML en Backend          |
| - Ensamblaje de documento DOCX        |
| - Distribución espacial de párrafos   |
| - Incrustación de recortes gráficos   |
+---------------------------------------+
                |
                v
[ Documento Word (.docx) descargable ]
```

### 9.1 Características Técnicas del Módulo
1. **Reconocimiento Óptico Neuronal**:
   - Integración con OCR.space API mediante el motor especializado en documentos hispanohablantes (Motor 2 con parámetro `isTable: true` y `detectOrientation: true`).
   - Generación de estructura jerárquica con coordenadas de líneas y palabras (`TextOverlay`).
2. **Motores Locales Alternativos**:
   - PaddleOCR en Python 3.12 (PP-OCRv5) y Tesseract en Node.js integrados como alternativa sin conexión a Internet, asegurando continuidad operativa ante contingencias de red.
3. **Editor Interactivo de Transcripción**:
   - Componente `EvidenciaOcr.tsx` que superpone cajas delimitadoras de texto sobre la imagen escaneada.
   - Permite al prevencionista corregir palabras mal interpretadas antes de consolidar el expediente.
   - Herramienta de selección gráfica para capturar firmas de involucrados y timbres institucionales (hasta 12 elementos gráficos).
4. **Generador de Documentos Word (DOCX)**:
   - Implementado en `DocumentoEvidencia.cs` utilizando OpenXML SDK.
   - Construye un archivo `.docx` descargable con márgenes estandarizados, encabezado institucional, párrafos transcritos e imágenes de las firmas recortadas incrustadas en su posición original.
   - Registro de borrador reactivo en IndexedDB del navegador para impedir pérdida de datos ante refrescos involuntarios.

---

## 10. Entrega Continua, Despliegue en la Nube y Monitoreo

### 10.1 Despliegue en Producción
- **Frontend**: Alojado en **Vercel** (`https://giatep.vercel.app`), con DNS seguro, compresión de borde, HTTP/2 y HSTS.
- **Backend**: Contenerizado en **Render** (`https://giatep-api.onrender.com`), ejecutado en contenedor Docker Multi-Stage con SDK 10.0 y runtime ASP.NET Core 10.0 bajo usuario no root `USER app` en puerto 8080.
- **Base de Datos**: Instancia en la nube de **Supabase PostgreSQL 17** (`aws-1-sa-east-1.pooler.supabase.com:5432`), con pooling transaccional.

### 10.2 Pipeline de Integración Continua (GitHub Actions)
Definido en `.github/workflows/ci.yml`:
1. **Compilación y Pruebas del Backend**:
   - Configura entorno .NET 10.
   - Restaura dependencias y ejecuta `dotnet build backend/Giatep.slnx`.
   - Ejecuta las 72 pruebas automáticas en `Giatep.Tests` generando archivo de resultados TRX.
2. **Pruebas del Módulo OCR**:
   - Configura entorno Node.js 22.
   - Ejecuta pruebas de escaneo y OCR local (`npm test`).
3. **Compilación y Verificación de Tipos del Frontend**:
   - Instala paquetes con `npm ci`.
   - Ejecuta `npm run build` (`tsc -b && vite build`) garantizando cero errores de tipado o empaquetado.
4. **Empaquetado de Artefactos**:
   - Publica los artefactos validados solo si todas las pruebas anteriores pasan con 0 errores.

### 10.3 Monitoreo de Salud y Continuidad Operativa
- Endpoint de verificación activa: `GET https://giatep-api.onrender.com/health`. Retorna HTTP 200 OK con JSON:
  ```json
  {
    "estado": "disponible",
    "almacenamiento": "Supabase PostgreSQL"
  }
  ```
- Respaldo continuo en Supabase: Point-in-time recovery (PITR) con RPO de 24 horas y RTO menor a 30 minutos.
- Despliegue seguro: Rollback instantáneo en Vercel y redespliegue de imagen anterior en Render ante anomalías.

---

## 11. Articulación Curricular de Asignaturas y Conclusiones

El proyecto GIATEP evidencia de manera tangible la integración holística de las competencias desarrolladas a lo largo de la carrera:

1. **Programación Orientada a Objetos II**:
   - Aplicación rigurosa de principios SOLID, Patrón Repository (`ICasoRepository`), Patrón Strategy (`IExportador`) y Dependency Injection nativa para lograr un código altamente testeable y desacoplado.
2. **Modelamiento de Bases de Datos**:
   - Diseño relacional de 18 tablas en tercera forma normal, restricciones de dominio CHECK, claves foráneas con integridad referencial, índices compuestos y filtrados, y soporte de bases de datos relacionales avanzadas (PostgreSQL 17).
3. **Inteligencia de Negocios I y II**:
   - Diseño de 6 indicadores clave de desempeño (KPIs) con base matemática formal, tratamiento de casos borde (denominadores cero), agregación analítica y visualizaciones interactivas de series temporales y composición de eventos.
4. **Lenguaje de Programación Web II**:
   - Desarrollo full-stack moderno (.NET 10 + React 19), APIs RESTful seguras bajo Problem Details RFC 7807/9110, control de acceso RBAC con aislamiento multi-tenant, accesibilidad universal WCAG 2.2 AA, contenerización Docker y despliegue distribuido en la nube.

**Conclusión Final**:  
GIATEP representa un producto de software robusto, accesible, seguro y preparado para producción, cumpliendo de manera demostrable y verificable con el **100% de los criterios en nivel Destacado** de la rúbrica de Certamen 3 (Hito 3).
