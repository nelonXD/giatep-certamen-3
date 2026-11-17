> Actualización del 8 de noviembre de 2026: se implementó OCR local con un modelo neuronal preentrenado de Tesseract en español. En Evidencias de investigaciones abiertas permite reconocer una imagen PNG/JPG, revisar el texto y crear un Word asociado a la fuente. No usa OpenAI, no detecta objetos ni genera conclusiones. La IA generativa o detección de objetos mencionada como pendiente en versiones anteriores no forma parte de este OCR. Consulta el README para instalación y límites.

## Verificación de OCR local · 8 de noviembre de 2026

- Frontend: `npm run build` correcto después de incorporar la pantalla de escaneo y revisión.
- Modelo: `npm test` en `backend/ocr`, dos pruebas correctas. Lectura real en español de una imagen sintética y rechazo de contenido inválido.
- Backend: 46 pruebas correctas, 0 fallidas y 3 omitidas de SQL con activación especial. Incluye proceso OCR real, permisos, pertenencia de evidencia, bloqueo de investigación cerrada, validación del texto, documento DOCX descargable, auditoría y conservación del original.
- NuGet emitió advertencias NU1900 porque no fue posible consultar su índice de vulnerabilidades; no fueron errores de compilación.
- No se completó un recorrido visual del nuevo formulario en navegador ni una prueba con SQL Server en esta revisión. No se presenta la prueba con base InMemory como validación de SQL.

> Actualización vigente del 2 de noviembre de 2026: la investigación es independiente del caso, con entrevistas, evidencia, relato final, hechos, árbol y medidas. El plan se inicia después de cerrar la investigación. El detalle y las reglas que reemplazan el flujo anterior están en INVESTIGACIONES.md.

# Validación de la entrega

Fecha: 1 de noviembre de 2026. Las evidencias corresponden a este equipo local con SQL Express, API .NET y Chromium. No se atribuyen pruebas a herramientas o personas que no las ejecutaron.

## Resultados automáticos

- Backend: compilación sin errores ni advertencias. Veinte pruebas de negocio e integración correctas en backend.trx. La prueba SQL de recuperación se omite por defecto en esta suite y se ejecuta de manera independiente.
- Recuperación: una prueba SQL real correcta en recuperacion.trx. restauracion.json registra CHECKSUM, conteos iguales de origen y copia y nombre del respaldo. No se reemplazó la base original.
- Frontend: TypeScript y compilación de producción correctos.
- Navegador: seis recorridos correctos con Playwright: dashboard responsive, expediente completo con evidencia y cierre, nueve exportaciones e impresión, administración/auditoría, Jefatura y flujo de reportes con teclado.
- Accesibilidad: once auditorías axe con cero infracciones detectadas en las pantallas recorridas. Se conserva un JSON por pantalla. Esto no certifica toda WCAG ni sustituye evaluación humana.
- Responsive: capturas de dashboard a 360, 768 y 1440 px; comprobación automática de ausencia de desbordamiento horizontal de la página. Reflow equivalente a 200% en 720 px sobre base de 1440 px.
- Teclado: login, navegación, selección de reporte y descarga completos sin clicks ni foco programático. Evidencia en teclado.json. Las confirmaciones del caso se recorrieron también como parte de la prueba del expediente.
- Archivos: los nueve ejemplos exportados se abrieron con lectores de PDF y XLSX. Los PDF contienen texto seleccionable; los XLSX contienen filas y tipos. El resumen gerencial incluye totales y representación gráfica. Vista de impresión en PDF y PNG.
- Dependencias frontend: la instalación reportó cero vulnerabilidades conocidas. El proyecto actualiza explícitamente la dependencia criptográfica que produjo una alerta durante la construcción inicial. Las auditorías son verificaciones de un momento, no una garantía permanente.

## Ubicación de evidencia

backend.trx, recuperacion.trx y frontend.json en evidencias. Auditorías axe-*.json. Capturas dashboard-360.png, dashboard-768.png y dashboard-1440.png. exportaciones contiene los tres reportes en los tres formatos. impresion-gerencial.pdf/png y teclado.json completan la demostración. Los archivos de trazas del navegador permanecen excluidos de Git porque pueden contener información de sesiones locales.

## Revisión humana pendiente antes de la defensa

No se detectó una instalación de NVDA en las ubicaciones estándar del equipo. Debe ejecutarse una prueba con NVDA o VoiceOver y registrar fecha, versión, rol, recorrido y resultado. La lectura del árbol de accesibilidad y axe no se presentan como uso de un lector de pantalla.

Protocolo: iniciar sesión, localizar región principal y encabezados, leer etiquetas y errores de un caso inválido, recorrer registro o reporte con teclado, abrir la tabla de un gráfico, descargar un reporte y confirmar que los mensajes se anuncian. Verificar foco después de navegación, controles de fecha, confirmaciones nativas y retorno a contenido. Corregir y repetir si existe un bloqueo.

También requieren evidencia humana la aprobación del dominio, nombres y aportes del equipo, ejecución remota del pipeline tras publicar el repositorio y la defensa individual. El informe y el material están preparados; no se inventan aprobaciones, commits de terceros ni una presentación ya realizada.

## Excepciones acordadas

Docker se excluye por indicación del profesor comunicada por el usuario. La IA externa permanece sin implementar por solicitud del usuario. La cuenta SQL local proporcionada por el usuario no se usa como cuenta de producción propuesta.

## Verificación de investigaciones del 2 de noviembre

La suite investigaciones.trx valida el inicio único, secciones obligatorias, rechazo de ciclos, cierre de investigación sin cerrar el caso, bloqueo de edición posterior y activación única de medidas. El recorrido Playwright actualizado completa todas las secciones, activa el plan y cierra el caso en SQL real. Las auditorías axe-investigacion-*.json y la captura investigacion-arbol.png corresponden a las pantallas nuevas. Antes de migrar se guardó y verificó un respaldo (respaldo-antes-investigaciones.trx).
