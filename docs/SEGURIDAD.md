> Actualización del 8 de noviembre de 2026: se implementó OCR local con un modelo neuronal preentrenado de Tesseract en español. En Evidencias de investigaciones abiertas permite reconocer una imagen PNG/JPG, revisar el texto y crear un Word asociado a la fuente. No usa OpenAI, no detecta objetos ni genera conclusiones. La IA generativa o detección de objetos mencionada como pendiente en versiones anteriores no forma parte de este OCR. Consulta el README para instalación y límites.

# Seguridad y evidencia de mitigaciones

| Riesgo OWASP API | Control implementado | Evidencia |
| --- | --- | --- |
| API1 Autorización por objeto rota | AccesoService limita casos por centro, incluidos detalle, evidencias, análisis y exportación | ApiTests.JefaturaNoVeOtroCentro y prueba E2E de Jefatura |
| API2 Autenticación rota | Hash de contraseña, JWT con emisor/audiencia/vigencia, cuenta activa comprobada y bloqueo por fallos | AuthEndpoints y validación JWT en Program |
| API3 Autorización de propiedades rota | DTO explícitos; no se aceptan Estado/Actor/PasswordHash desde el formulario de caso; respuestas de usuarios públicas | DTO, AuthEndpoints.Publico, GestionEndpoints |
| API4 Consumo no restringido | Rate limiter global, login y exportación; 100 filas/página; 5 MB por adjunto; 20.000 filas/exportación | Program, CasoEndpoints, ReporteEndpoints |
| API5 Autorización de función rota | Administrador, Prevencion y Operacion con políticas del servidor | Pruebas PrevencionNoAdministra, AdminNoInvestiga, JefaturaNoCreaCasos |
| API8 Configuración insegura | Secretos fuera de Git, no-store, nosniff, DENY y misma procedencia web/API | .gitignore, Program, configuración Vite y plan HTTPS |
| API9 Inventario incorrecto | Rutas /api/v1, contrato OpenAPI exportado y documentación interactiva en desarrollo | docs/openapi.json y /scalar/v1 |

La matriz no afirma cobertura exhaustiva de todas las vulnerabilidades OWASP. No se ha realizado una prueba de penetración externa. Los entornos de desarrollo son HTTP local; un despliegue debe aplicar HTTPS en el proxy y cuentas SQL mínimas, sin sa.

Los logs no incluyen contraseñas ni tokens. La auditoría excluye PasswordHash y Contenido. Se conservan actor, acción, campos modificados y fecha para cambios; las exportaciones registran formato, filtros y cantidad de filas. Las descargas de evidencia también quedan auditadas.

La validación de firma de archivos evita formatos triviales falsos, pero no equivale a análisis antivirus. Los PDF se descargan como adjuntos. La integración de IA está ausente y no envía documentos a terceros.
