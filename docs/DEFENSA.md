# Guía Exhaustiva para la Defensa Oral y Demostración en Vivo
## Sistema GIATEP · Certamen 3 (Hito 3) · Criterio Destacado (100%)

Esta guía técnica está diseñada para orientar y preparar a los estudiantes durante la presentación oral y demostración en vivo (Live Demo) ante la comisión evaluadora de Lenguaje de Programación Web II.

---

## 1. Preparación Previa y Recomendaciones para la Demostración

1. **Protocolo de "Warm-Up" en Producción (5 minutos antes)**:
   - Render Free Tier suspende contenedores tras 15 minutos de inactividad.
   - Ejecutar una petición GET en el navegador o consola a `https://giatep-api.onrender.com/health` 5 minutos antes de la defensa. Esto asegura que el contenedor Docker esté activo y caliente, respondiendo en menos de 200 milisegundos durante la presentación.
2. **Credenciales de Demostración**:
   - **Administrador**: `admin` / `Admin123!` (Gestión de usuarios y maestros institucionales).
   - **Prevención de Riesgos**: `prevencion` / `Prev123!` (Gestión transversal de casos, investigaciones, OCR y planes).
   - **Jefatura de Centro**: `jefatura` / `Jefa123!` (Confinado exclusivamente al Hospital Dr. Sótero del Río).
3. **Evidencias de Prueba Preparadas**:
   - Imagen de documento de prueba: `backend/ocr/test-fixtures/documento.png` o cualquier imagen escaneada ligera (PNG/JPG <= 1 MB) que contenga texto, sellos y firmas.
4. **Verificación de Navegación por Teclado**:
   - Demostrar el uso de la tecla `Tab` para recorrer controles interactivos, el salto al contenido (`Skip link`), el foco visible y el uso de `Escape` en modales.

---

## 2. Guion de Demostración en Vivo (Live Demo Paso a Paso · 20 Minutos)

### Minuto 0 a 2: Introducción, Propósito y Arquitectura en la Nube
- **Acción**: Proyectar la pantalla de inicio de sesión en `https://giatep.vercel.app` y presentar la diapositiva de portada.
- **Relato**:
  > "Buenas tardes estimados profesores de la comisión evaluadora. Hoy presentamos GIATEP para el Certamen 3 (Hito 3). GIATEP es un sistema integral para la gestión e investigación de accidentes del trabajo y enfermedades profesionales bajo la normativa chilena SUSESO. La aplicación está completamente desplegada en la nube con una arquitectura distribuida: Single Page Application en Vercel Edge Network, backend en .NET 10 contenerizado con Docker Multi-Stage en Render y base de datos relacional de alta disponibilidad en Supabase PostgreSQL 17."

### Minuto 2 a 4: Rol Administrador y Segregación de Responsabilidades
- **Acción**: Iniciar sesión con el usuario `admin`.
- **Navegación**: Ir al módulo de **Administración** (`/admin`).
- **Demostración**:
  - Mostrar la gestión de establecimientos de salud, empleadores y trabajadores.
  - Explicar la política de mínimos privilegios: el Administrador tiene acceso exclusivo a maestros y roles, pero está estrictamente bloqueado de crear o manipular investigaciones operativas de prevención (`AdminNoInvestiga`).
- **Cierre del Bloque**: Cerrar sesión (`admin`).

### Minuto 4 a 8: Rol Prevención — Registro de Denuncia (DIAT/DIEP) y Autocompletado RUN
- **Acción**: Iniciar sesión con el usuario `prevencion`.
- **Navegación**: Ir a **Casos** (`/casos`) y pulsar **Nuevo Caso**.
- **Demostración**:
  - Abrir el diálogo modal accesible (`SeleccionTipoCaso.tsx`) y elegir **Accidente del Trabajo (DIAT)**.
  - Demostrar el autocompletado reactivo de RUN chileno (`RutRegistroField.tsx`): ingresar un RUN existente (ej. `15.234.567-8`) o seleccionar una sugerencia; observar cómo los datos del trabajador y empleador se completan automáticamente sin recargar la página.
  - Completar los datos del evento (fecha, relato de más de 20 caracteres) y guardar. El caso queda en estado inicial `Borrador`.
  - Demostrar la transición: pulsar **Iniciar Investigación**. El sistema crea la entidad independiente `Investigacion`, genera un snapshot inmutable de antecedentes y avanza el estado del caso a `EnInvestigacion`.

### Minuto 8 a 11: Rol Prevención — Digitalización con Inteligencia Artificial (OCR) y Generación de DOCX
- **Acción**: Dentro de la investigación recién creada, navegar a la pestaña **Evidencias y OCR**.
- **Demostración**:
  - Cargar una imagen de evidencia escaneada (`documento.png`).
  - Pulsar **Procesar con IA / OCR**: el backend invoca el motor neuronal de OCR.space (o motor local de respaldo), analizando el texto con TextOverlay en español.
  - Mostrar el **Editor Interactivo de Transcripción**: corregir en vivo una palabra o párrafo del texto reconocido.
  - **Recorte Gráfico**: Utilizar el cursor del mouse o los campos numéricos de accesibilidad (`x`, `y`, `ancho`, `alto`) para seleccionar el recuadro de la firma del trabajador y el timbre de la mutualidad. Destacar el cumplimiento del criterio WCAG 2.2 SC 2.5.7 (Alternativa a movimientos de arrastre).
  - Pulsar **Generar Documento Word**: el backend utiliza la librería OpenXML (`DocumentoEvidencia.cs`) para ensamblar y descargar un archivo `.docx` estructurado con la distribución espacial del documento original y los recortes gráficos incrustados.

### Minuto 11 a 13: Rol Prevención — Entrevistas, Hechos y Árbol de Causas (DAG)
- **Acción**: Navegar a la pestaña **Entrevistas y Causas**.
- **Demostración**:
  - Registrar una declaración testimonial con calidad `Testigo`.
  - Construir el **Árbol de Causas**: registrar hechos antecedentes y definir exactamente un hecho final.
  - Trazar relaciones causales dirigidas. Destacar que el algoritmo en backend (`Alcanza`) impide la creación de ciclos o autorrelaciones (`CK_Relaciones_NoAutoRelacion`), cumpliendo formalmente con la estructura matemática de un Grafo Acíclico Dirigido (DAG).
  - Mostrar la visualización gráfica SVG y desplegar la tabla semántica accesible alternativa (`<details className="chart-table">`).
  - Vincular una medida preventiva correctiva a uno de los hechos antecedentes, asignándola a un responsable de Jefatura.
  - Pulsar **Cerrar Investigación**: el backend valida los 7 requisitos y cambia el estado a `Cerrada`.
  - Pulsar **Activar Plan de Acción**: las medidas se materializan en la tabla `Medidas` y el caso transiciona a `EnSeguimiento`.
- **Cierre del Bloque**: Cerrar sesión (`prevencion`).

### Minuto 13 a 15: Rol Jefatura — Aislamiento Multi-Tenant y Cumplimiento de Medidas
- **Acción**: Iniciar sesión con el usuario `jefatura`.
- **Demostración**:
  - **Aislamiento Multi-Tenant**: Mostrar que en el listado de casos solo figuran los registros pertenecientes a su establecimiento asignado (Hospital Dr. Sótero del Río). Explicar que cualquier intento de acceder a un ID de otro centro vía URL devuelve HTTP 404 Problem Details gracias a `AccesoService.cs` (mitigación de BOLA / OWASP API 1).
  - Mostrar que Jefatura no tiene permisos para crear casos (`JefaturaNoCreaCasos`).
  - Navegar a **Planes de Acción** (`/planes`): localizar la medida preventiva asignada a su cargo.
  - Reportar cumplimiento de la medida: ingresar descripción de las acciones tomadas en terreno y adjuntar documento de respaldo de entrega.
- **Cierre del Bloque**: Cerrar sesión (`jefatura`).

### Minuto 15 a 17: Rol Prevención — Verificación y Cierre Estricto del Caso
- **Acción**: Iniciar sesión nuevamente con `prevencion`.
- **Demostración**:
  - Navegar al plan de acción del caso en seguimiento.
  - Intentar cerrar el caso antes de verificar la medida: demostrar que el sistema o el botón bloquea el cierre prematuro (gating de cierre en `CasoService.cs:35-38`).
  - Revisar la evidencia entregada por Jefatura y pulsar **Aprobar Verificación**.
  - Ahora que el 100% de las medidas del plan están completadas y verificadas, pulsar **Cerrar Caso**.
  - El caso transiciona a estado `Cerrado`, se registra la `FechaCierre` oficial y el expediente completo queda en modo **Solo Lectura**, garantizando la inmutabilidad de los registros históricos.

### Minuto 17 a 19: Analítica Preventiva, Dashboard Horizon UI y Reportes Auditados
- **Acción**: Navegar al **Dashboard** (`/dashboard`).
- **Demostración**:
  - Mostrar las 6 tarjetas de KPIs preventivos, señalando las fórmulas explícitas en cada tarjeta.
  - Demostrar el comportamiento de los filtros por fecha (`desde`, `hasta`) y por centro de salud: los 4 gráficos interactivos (líneas, donut, barras, avance) se recalculan dinámicamente.
  - Abrir una tabla semántica desplegable (`<details>`) para demostrar la accesibilidad para usuarios con lectores de pantalla.
  - Navegar a **Reportes** (`/reportes`):
    - Seleccionar **Reporte Gerencial** y exportar a **Excel (XLSX)**: abrir el archivo y mostrar que contiene tipos numéricos reales y estilos.
    - Exportar a **PDF**: abrir el archivo y mostrar la paginación vectorial y membrete institucional.
    - Exportar a **CSV**: explicar que se genera en UTF-8 con BOM y sanitización contra inyección de fórmulas.
    - Abrir la **Vista de Impresión**: mostrar cómo las reglas `@media print` ocultan la barra superior y botones, dejando un informe formal limpio.

### Minuto 19 a 20: Verificación Técnica de la Infraestructura en la Nube
- **Acción**: Abrir una terminal o pestaña del navegador e invocar:
  ```powershell
  curl.exe -i https://giatep-api.onrender.com/health
  ```
- **Demostración**:
  - Mostrar la respuesta HTTP 200 OK:
    ```json
    {
      "estado": "disponible",
      "almacenamiento": "Supabase PostgreSQL"
    }
    ```
  - Destacar las cabeceras de seguridad verificables: `x-content-type-options: nosniff`, `x-frame-options: DENY`, `referrer-policy: no-referrer`, `Cache-Control: no-store`.
  - Mencionar que las 72 pruebas automáticas en `backend/Giatep.Tests` se encuentran pasando al 100% en el pipeline de GitHub Actions.
- **Cierre**: Dar paso a la ronda de preguntas de la comisión evaluadora.

---

## 3. Banco de 15 Preguntas Técnicas Frecuentes (FAQ) con Fundamentación en Código Fuente

### Pregunta 1: ¿Qué mecanismo impide que un caso se cierre con medidas preventivas pendientes o incompletas?
- **Respuesta**: El cierre de un caso está protegido a nivel de servicio de dominio en `backend/Giatep.Api/Services/CasoService.cs` (líneas 35-38). Antes de actualizar el estado a `Cerrado`, el método valida:
  ```csharp
  if (caso.Medidas.Count == 0 || !caso.Medidas.All(m => m.Completada))
      throw new ReglaNegocioException("No se puede cerrar el caso: todas las medidas del plan deben estar completadas y verificadas.");
  ```
  Si existe al menos una medida pendiente o devuelta, se lanza una `ReglaNegocioException`, la cual es interceptada por el middleware global de ASP.NET Core (`Program.cs:115`) y traducida a una respuesta HTTP 409 Conflict bajo el estándar Problem Details RFC 7807. Adicionalmente, el frontend deshabilita el botón de cierre en `PlanesPage.tsx:30` hasta que `verificadas === plan.medidas.length`.

### Pregunta 2: ¿Por qué ocultar o deshabilitar un botón en la interfaz de usuario no garantiza la seguridad del sistema?
- **Respuesta**: La interfaz de usuario (React) corre en el navegador del cliente y puede ser manipulada mediante herramientas de desarrollador (F12) o atacada directamente con herramientas HTTP como Postman o cURL. Toda regla de autorización y control de estados debe ser validada **estrictamente en el backend**. En GIATEP, los endpoints aplican filtros de políticas (`.RequireAuthorization("Prevencion")` en `CasoEndpoints.cs:64`) y la lógica de dominio en `CasoService.cs` valida las precondiciones independientemente de la petición que llegue desde el cliente.

### Pregunta 3: ¿Cómo se implementa el aislamiento multi-tenant para que un usuario de Jefatura no pueda ver ni descargar evidencias de otro centro de salud (BOLA)?
- **Respuesta**: Se implementa a través de la clase `AccesoService` (`backend/Giatep.Api/Services/AccesoService.cs`, líneas 9-15). La propiedad `Casos` evalúa si el usuario en sesión tiene el rol `Jefatura`; de ser así, aplica un filtro LINQ automático por su claim de token `establecimiento`:
  ```csharp
  public IQueryable<CasoRegistro> Casos => EsJefatura ? db.Casos.Where(c => c.EstablecimientoId == EstablecimientoId) : db.Casos;
  public CasoRegistro Exigir(Guid id) => Casos.AsNoTracking().SingleOrDefault(c => c.Id == id) 
      ?? throw new KeyNotFoundException("No se encontró el caso en tu ámbito de acceso.");
  ```
  Al intentar consultar un caso, evidencia o reporte de otro centro, `Exigir(id)` no encuentra el registro en su ámbito y lanza `KeyNotFoundException`, retornando HTTP 404 Problem Details sin filtrar si el recurso existe o no en otro establecimiento.

### Pregunta 4: ¿Qué ventajas aporta la utilización del Patrón Repository (`ICasoRepository`) en este proyecto?
- **Respuesta**: El Patrón Repository (`backend/Giatep.Api/Repositories/ICasoRepository.cs`) abstrae el mecanismo de persistencia respecto a las reglas de negocio en `CasoService`. Esto permite desacoplar completamente Entity Framework Core del dominio, posibilitando la ejecución de pruebas unitarias ultrarrápidas mediante implementaciones en memoria (`CasoMemoriaRepository`), sin depender de un motor SQL levantado ni lidiar con bloqueos de base de datos durante la ejecución de la suite de pruebas.

### Pregunta 5: ¿Cómo maneja el sistema la revocación inmediata de sesiones activas ante cambios de roles o contraseñas?
- **Respuesta**: En `backend/Giatep.Api/Program.cs` (líneas 81-92), en el evento `OnTokenValidated` de la autenticación JWT, se consulta en tiempo real a la base de datos la cuenta del usuario:
  - Se comprueba que la cuenta continúe con `Activo == true`.
  - Se valida que el claim `rol` coincida con el rol en la base de datos.
  - Se calcula el hash SHA-256 del `PasswordHash` actual y se compara con el claim `version` emitido en el token.
  Si el administrador cambia el rol, desactiva la cuenta o el usuario restablece su clave, la siguiente petición rechaza el token con HTTP 401 Unauthorized de forma inmediata, sin esperar los 30 minutos de expiración natural del JWT.

### Pregunta 6: ¿Cómo se garantiza matemáticamente que el Árbol de Causas no contenga ciclos infinitos ni autorrelaciones?
- **Respuesta**: En `backend/Giatep.Api/Services/InvestigacionService.cs` (líneas 66-84):
  1. Se valida que `antecedenteId != consecuenciaId` tanto en código como en la restricción SQL CHECK `CK_Relaciones_NoAutoRelacion`.
  2. Antes de persistir una nueva arista causal, se ejecuta un algoritmo de búsqueda en profundidad (DFS) en la función local `Alcanza(consecuenciaId, antecedenteId, relaciones)`. Si desde la consecuencia ya es posible alcanzar al antecedente, agregar la arista formaría un ciclo; en tal caso, se arroja `ReglaNegocioException("La relación generaría un ciclo en el árbol causal.")`.

### Pregunta 7: ¿Qué ocurre cuando el denominador de un indicador analítico es igual a cero (por ejemplo, si no hay casos cerrados en el período)?
- **Respuesta**: En `backend/Giatep.Api/Services/AnalisisService.cs`, cada fórmula operativa controla explícitamente la división por cero:
  - Para **Proporción Cerrada**: si `totalCasos == 0`, retorna exactamente `0.0`.
  - Para **Tiempo Medio de Cierre**: si la cantidad de casos cerrados en el rango es cero, retorna `0.0` sin intentar promediar días.
  Esto previene excepciones de runtime tipo `DivideByZeroException` o valores `NaN` / `Infinity` en las respuestas JSON del dashboard.

### Pregunta 8: ¿Cómo se previene el riesgo de ataques de inyección en las exportaciones de archivos CSV (CSV Formula Injection)?
- **Respuesta**: En `backend/Giatep.Api/Services/Exportadores.cs` (`CsvExportador`), al serializar cada celda de texto, se evalúa si el primer carácter es uno de los prefijos ejecutables en programas como Microsoft Excel (`=`, `+`, `-`, `@`). Si es así, se sanitiza anteponiendo una comilla simple (`'`), desactivando la ejecución automática de fórmulas o macros maliciosas al abrir el archivo en la estación del usuario. Adicionalmente, se incluye el BOM UTF-8 (`EF BB BF`) para compatibilidad de caracteres en español.

### Pregunta 9: ¿Por qué se optó por Supabase PostgreSQL 17 en producción y cómo se mantiene la compatibilidad con SQL Server en desarrollo?
- **Respuesta**: Supabase PostgreSQL 17 ofrece alta disponibilidad en la nube, connection pooling transaccional con PgBouncer, tipos UUID nativos y excelente desempeño sin costos de licencias propietarias. La bivalencia se gestiona en `backend/Giatep.Api/Models/GiatepDbContext.cs` mediante la condición `if (Database.IsNpgsql())`:
  - Para PostgreSQL: configura tipos `uuid`, `timestamp with time zone` y nombres de índices compatibles con Postgres.
  - Para SQL Server: configura `uniqueidentifier`, `datetimeoffset` y sintaxis T-SQL para índices filtrados (`WHERE [EsFinal] = 1`).
  Esto permite desarrollar localmente con SQL Server o Postgres y desplegar en la nube sobre Supabase sin fricción.

### Pregunta 10: ¿Cómo funciona el módulo de digitalización documental asistido por IA (OCR)?
- **Respuesta**: El módulo implementa una arquitectura híbrida (`EvidenciaOcrEndpoints.cs` y `OcrSpaceService.cs`):
  1. Recibe una imagen escaneada (PNG/JPG <= 1 MB) y la envía al servicio neuronal de OCR.space con motor especializado en español y detección de tablas (`isTable: true`).
  2. OCR.space retorna un JSON con el texto transcrito y el array `TextOverlay` con coordenadas de líneas y palabras.
  3. En caso de falla de red o contingencia, el sistema puede derivar la imagen a motores locales de respaldo (PaddleOCR o Tesseract).
  4. La UI de React (`EvidenciaOcr.tsx`) presenta el texto reconocido para corrección humana y permite seleccionar zonas gráficas para recortar firmas y sellos.
  5. Finalmente, el backend con OpenXML (`DocumentoEvidencia.cs`) sintetiza un archivo Word (.docx) descargable con la estructura del documento y las firmas incrustadas.

### Pregunta 11: ¿Qué medidas de mitigación se aplican contra ataques de consumo desmedido de recursos (OWASP API 4)?
- **Respuesta**: Se implementan múltiples capas de contención:
  1. **Rate Limiting Granular** (`Program.cs:99-108`): 240 req/min global por IP, 20 req/min para login, 30 req/min para descargas de reportes y 6 req/min para escaneos OCR.
  2. **Límites de Carga**: Kestrel limita el cuerpo HTTP a 6 MB; las evidencias documentales a 5 MB y el payload OCR a 1 MB.
  3. **Paginación Forzada**: Consultas SQL limitadas a un máximo de 100 registros por página (`tamano <= 100`).
  4. **Límite de Exportación**: Máximo 20.000 filas en reportes consolidados.

### Pregunta 12: ¿Cómo se asegura la integridad de los archivos de evidencias almacenados en el disco del servidor?
- **Respuesta**: Al subir un archivo (`EvidenciasEndpoints.cs`), se verifican sus *magic bytes* para comprobar que el contenido coincida con el tipo MIME declarado (PDF, PNG o JPEG). Se calcula el hash criptográfico SHA-256 de los bytes y se persiste en la columna `Sha256` (CHAR 64). Al descargar el archivo, el sistema recalcula el SHA-256; si los hashes difieren (evidencia de manipulación o corrupción en disco), se aborta la entrega arrojando una excepción de seguridad. Adicionalmente, la ruta se resuelve con `Path.GetFullPath` validando que pertenezca al directorio de almacenamiento para prevenir ataques de *Path Traversal* (`../`).

### Pregunta 13: ¿Cómo cumple la solución con el criterio WCAG 2.2 SC 2.4.11 (Focus Not Obscured) frente a la barra superior fija?
- **Respuesta**: La barra superior (`.topbar`) tiene una altura mínima de 82px con posición fija en la parte superior (`position: sticky; top: 0;`). Para evitar que al tabular o navegar mediante enlaces ancla un elemento con foco quede parcialmente cubierto por la barra, se incorporó en `frontend/src/index.css`:
  ```css
  html {
    scroll-padding-top: 90px;
  }
  ```
  Esto instruye al motor de renderizado del navegador a mantener un margen superior de 90px al desplazar cualquier elemento al área visible, garantizando que el elemento enfocado sea 100% visible sin ser ocultado.

### Pregunta 14: ¿Cómo está configurada la imagen Docker Multi-Stage y por qué corre bajo el usuario `app`?
- **Respuesta**: La imagen se construye en dos fases en `Dockerfile`:
  - **Fase de build**: Usa la imagen pesada `mcr.microsoft.com/dotnet/sdk:10.0` para restaurar dependencias NuGet y compilar los binarios en modo Release.
  - **Fase de runtime**: Copia únicamente la carpeta publicada hacia la imagen mínima `mcr.microsoft.com/dotnet/aspnet:10.0`.
  Se declara explícitamente la directiva `USER app`, que utiliza una cuenta de sistema preconfigurada sin privilegios de superusuario (no root). Esto garantiza que, si ocurriese una vulnerabilidad de ejecución remota de código en alguna dependencia de terceros, el atacante no tendría privilegios root para acceder al sistema operativo del host en Render.

### Pregunta 15: ¿Cuál es el procedimiento de recuperación y continuidad operativa ante una falla catastrófica de datos (RPO y RTO)?
- **Respuesta**: La estrategia de continuidad operativa se apoya en Supabase PostgreSQL:
  - **RPO (Recovery Point Objective)**: Menor a 1 hora gracias a los snapshots continuos y al registro de transacciones WAL (Write-Ahead Logging) con Point-in-Time Recovery (PITR) de 7 días.
  - **RTO (Recovery Time Objective)**: Menor a 30 minutos, tiempo necesario para aprovisionar una nueva instancia o revertir el estado de la base al segundo anterior a la falla.
  - En el backend, las migraciones son versionadas y simétricas (`schema_postgres.sql`), y las imágenes Docker anteriores están etiquetadas en Render para realizar un rollback inmediato en caso de fallos de compatibilidad en un nuevo release.
