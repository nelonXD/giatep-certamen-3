> Actualización del 8 de noviembre de 2026: se implementó OCR local con un modelo neuronal preentrenado de Tesseract en español. En Evidencias de investigaciones abiertas permite reconocer una imagen PNG/JPG, revisar el texto y crear un Word asociado a la fuente. No usa OpenAI, no detecta objetos ni genera conclusiones. La IA generativa o detección de objetos mencionada como pendiente en versiones anteriores no forma parte de este OCR. Consulta el README para instalación y límites.

> Actualización vigente del 2 de noviembre de 2026: la investigación es independiente del caso, con entrevistas, evidencia, relato final, hechos, árbol y medidas. El plan se inicia después de cerrar la investigación. El detalle y las reglas que reemplazan el flujo anterior están en INVESTIGACIONES.md.

# Requerimientos e historias de usuario

## Contexto y alcance

GIATEP centraliza accidentes del trabajo y enfermedades profesionales en una red de establecimientos. La versión académica usa personas y eventos sintéticos. Los formatos son una adaptación del dominio, no una reproducción de formularios oficiales: los anexos 14.1 y 14.2 no fueron suministrados. El proyecto no sustituye la evaluación profesional ni afirma cumplimiento legal específico.

El usuario solicitó tres roles y un flujo simplificado. Docker está excluido por indicación del profesor comunicada por el usuario. La integración de IA está expresamente pendiente. No se incluye comité/delegado, alta dirección, notificación por correo, editor gráfico del árbol causal ni aprobación en cadena.

## Historias priorizadas

| ID | Prioridad | Rol e historia | Criterios de aceptación |
| --- | --- | --- | --- |
| HU01 | Alta | Como usuario, ingreso con mi cuenta | Credenciales válidas producen JWT de 30 minutos; las inválidas devuelven 401; cuentas inactivas no ingresan |
| HU02 | Alta | Como Prevención, registro un caso | Centro y trabajador activos y relacionados; fecha no futura; relato de 20 a 5.000 caracteres; estado inicial Borrador |
| HU03 | Alta | Como Prevención, investigo el evento | Inicio investigación desde Borrador; registro causas manuales de categorías definidas; adjunto PDF/JPG/PNG con contenido comprobado |
| HU04 | Alta | Como Prevención, defino el plan | Medida de 10 a 1.000 caracteres; responsable activo de Prevención o jefatura del centro; plazo no anterior al evento |
| HU05 | Alta | Como Jefatura, cumplo medidas | Veo únicamente casos de mi centro; solo completo medidas asignadas a mi cuenta y en seguimiento |
| HU06 | Alta | Como Prevención, cierro el expediente | Hay causas y medidas antes de seguimiento; todas las medidas deben estar completadas para cerrar; el caso cerrado es de solo lectura |
| HU07 | Media | Como usuario, coordino la gestión | Registro observaciones con autor y fecha en casos abiertos dentro de mi alcance |
| HU08 | Alta | Como usuario, consulto indicadores | Seis indicadores y cuatro visualizaciones desde SQL, filtros de fecha y centro, fórmulas y tablas accesibles |
| HU09 | Alta | Como usuario, exporto reportes | Tres reportes en XLSX, PDF, CSV e impresión; mismos filtros y permisos que la consulta; auditoría de cada exportación |
| HU10 | Alta | Como administrador, mantengo registros | Alta y edición de usuarios, centros y trabajadores; desactivación lógica sin borrar historial; restricciones ante medidas pendientes |
| HU11 | Media | Como administrador, reviso auditoría | Veo actor, fecha, acción, entidad y campos modificados, sin contraseñas ni contenido binario |
| HU12 | Media | Como usuario, cambio mi contraseña | Contraseña actual correcta, nueva de 12 a 200 caracteres y confirmación; el token anterior deja de ser válido |

## Reglas del dominio

1. Administrador administra y consulta análisis/reportes, pero no modifica investigaciones. Prevención opera todos los casos. Jefatura opera únicamente su centro.
2. Estados: Borrador, En investigación, En seguimiento, Cerrado. Solo se avanza al siguiente estado.
3. Las causas y nuevas medidas se registran durante investigación. Para iniciar seguimiento se exige al menos una causa y una medida.
4. El cumplimiento se registra durante seguimiento. Prevención puede verificar cualquier medida y Jefatura únicamente sus asignaciones.
5. El cierre requiere todas las medidas completadas. Se registra fecha de cierre y el expediente se conserva sin nuevas modificaciones.
6. Una cuenta con medidas pendientes no se desactiva ni cambia de ámbito. Un establecimiento con usuarios activos no se desactiva. El administrador no se retira a sí mismo su acceso.
7. Las bajas son lógicas para preservar referencias. Los casos no tienen eliminación pública.
8. Los filtros de análisis y reportes usan la fecha del evento; las medidas analizadas pertenecen a esos casos. Una medida está vencida si no está completada y su plazo es anterior a la fecha actual en Santiago.

## Requerimientos no funcionales verificables

| ID | Objetivo | Verificación |
| --- | --- | --- |
| RNF01 | JWT validado, permisos por rol y por objeto | Pruebas de API para 401, 403 y caso fuera del centro |
| RNF02 | Integridad en formulario, API y SQL | Etiquetas y límites; DTO y reglas; claves, longitudes, unicidad, checks y FK en migraciones |
| RNF03 | Listados de máximo 100 filas por página | Filtros, orden y paginación ejecutados en SQL; pruebas de entrada inválida |
| RNF04 | Interfaz de 360 a 1440 px sin desbordamiento de página | Capturas y comprobación de anchura; desplazamiento interno de tablas |
| RNF05 | Objetivo WCAG 2.2 AA | axe por pantallas, teclado, foco, tablas de gráficos y protocolo manual de lector |
| RNF06 | Datos persistentes y recuperables | Reinicio y restauración de respaldo en una base temporal separada |
| RNF07 | Exportación sin congelar el navegador | Descarga por HTTP con botón ocupado; procesamiento en servidor y límite de 20.000 filas |
| RNF08 | Secretos fuera del repositorio | .env excluido, plantilla sin valores reales y credenciales locales separadas |
| RNF09 | Objetivo de respuesta de 2 segundos con datos demo tras calentamiento | Debe medirse en el equipo de defensa; no constituye SLA garantizado |
| RNF10 | RPO 24 horas y RTO objetivo 30 minutos | Plan de respaldo diario, restauración probada y ensayo operativo |

## Trazabilidad académica

| Asignatura | Evidencia | Justificación |
| --- | --- | --- |
| OO II | CasoService, ICasoRepository e IExportador | Reglas encapsuladas, dependencia por contrato, Repository y Strategy |
| Base de Datos | GiatepDbContext, migraciones y prueba de restauración | Entidades relacionadas, integridad, consultas y recuperación |
| BI I | AnalisisService e indicadores con fórmulas | Preguntas de gestión e interpretación con límites explícitos |
| BI II | DashboardPage y gráficos accesibles | Filtros, agregaciones, narrativa y tablas de datos |
| Web II | React, API REST, JWT, pruebas y CI | Comunicación entre capas, seguridad y publicación condicionada a validación |
