> Actualización: Trabajadores, Empleadores, Casos y DenunciasCasos almacenan datos en tablas relacionadas. Registrar caso abre un diálogo previo de selección, sin selector de tipo dentro del formulario. Detalles vigentes en [Registro DIAT y DIEP](DENUNCIAS.md).

> Actualización vigente del 2 de noviembre de 2026: la investigación es independiente del caso, con entrevistas, evidencia, relato final, hechos, árbol y medidas. El plan se inicia después de cerrar la investigación. El detalle y las reglas que reemplazan el flujo anterior están en INVESTIGACIONES.md.

# Arquitectura implementada de GIATEP

Presentación React/TypeScript con módulos por funcionalidad y componentes reutilizables. API ASP.NET Core con JWT, políticas y errores centralizados. Servicios de casos, acceso y análisis; Repository con EF Core/SQL Server; Strategy con exportadores CSV, XLSX y PDF. La inyección de dependencias registra cada contrato y la auditoría forma parte del guardado de cambios.

## Decisiones

ADR 001: conservar tecnologías del certamen 2. Se descarta Laravel/MySQL de la idea original para mantener un entorno conocido y una estructura coherente.

ADR 002: Repository separa CasoService de SQL. Se descarta incluir DbContext en la lógica del flujo. Un adaptador en memoria sirve para pruebas unitarias; la aplicación real usa SQL Server.

ADR 003: Strategy IExportador conserva un modelo de reporte común. Se descartan exportaciones independientes con filtros y permisos diferentes.

ADR 004: tres roles, sin comité ni delegado. Se simplifica a un flujo secuencial con causas y medidas como condiciones de avance y cierre.

ADR 005: nombres históricos como instantáneas y relaciones por GUID. Los análisis agrupan centros por identificador para evitar categorías duplicadas cuando cambia un nombre.

ADR 006: sin Docker por indicación docente comunicada por el usuario; ejecución directa y publicación de artefactos mediante CI. IA pendiente por solicitud expresa.

## Componentes y datos

Fuentes Mermaid y exportaciones SVG en diagramas/componentes, entidades, estados y despliegue. El diagrama de entidades muestra las relaciones operativas; Auditorias es un registro histórico desacoplado que conserva UsuarioId sin FK. Los detalles de tipos, índices, consultas y recuperación están en DICCIONARIO_DATOS e INFORME_TECNICO.

Los listados paginan en SQL. Los análisis materializan el volumen académico después de filtrar por centro y fechas. Las exportaciones tienen un máximo de 20.000 filas. Las lecturas analíticas y la validación de sesión usan operaciones asíncronas. La concurrencia avanzada sobre un expediente es una evolución pendiente de escala, no una garantía implementada.