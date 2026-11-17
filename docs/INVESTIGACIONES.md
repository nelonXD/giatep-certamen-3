> Actualización del 8 de noviembre de 2026: se implementó OCR local con un modelo neuronal preentrenado de Tesseract en español. En Evidencias de investigaciones abiertas permite reconocer una imagen PNG/JPG, revisar el texto y crear un Word asociado a la fuente. No usa OpenAI, no detecta objetos ni genera conclusiones. La IA generativa o detección de objetos mencionada como pendiente en versiones anteriores no forma parte de este OCR. Consulta el README para instalación y límites.

> Plan de acción vigente: menú propio, entrega con evidencia, aprobación o devolución por Prevención y cierre cuando todas las medidas estén verificadas. [Guía de uso](PLANES_ACCION.md).

> Actualización del 3 de noviembre: el registro de casos permite seleccionar accidente (DIAT) o enfermedad profesional (DIEP), completar los datos de su denuncia e iniciar la investigación desde el listado. Consulta [Registro DIAT y DIEP](DENUNCIAS.md).

# Investigación separada del caso

Actualización del 2 de noviembre de 2026 según la aclaración del usuario. Este documento reemplaza la descripción inicial que trataba investigación y plan como pestañas del mismo expediente.

## Ciclo operativo

1. Prevención registra el caso con los antecedentes iniciales del evento.
2. Desde el caso pulsa Iniciar investigación. El sistema crea un expediente independiente y conserva una instantánea de esos antecedentes. Cada caso admite una investigación.
3. La investigación aparece en su propio menú, con secciones de Antecedentes, Entrevistas, Evidencias, Relato final, Lista de hechos, Árbol de causas y Medidas.
4. Prevención cierra la investigación una vez completadas sus secciones. Este cierre bloquea la edición de la investigación y no cierra el caso.
5. Después pulsa Iniciar plan de acción. Las propuestas de la investigación se transfieren una sola vez a las medidas activas del caso, conservando responsable y plazo.
6. Prevención y los responsables autorizados registran cumplimiento. Al completar todas las medidas, Prevención cierra el caso.

## Requisitos de cierre

Se exige al menos una entrevista y una evidencia, relato final de veinte o más caracteres, al menos dos hechos con exactamente un hecho final, todos los hechos antecedentes conectados hacia ese hecho final y al menos una medida vinculada a un hecho antecedente. Los responsables deben estar activos y autorizados para el establecimiento.

Las entrevistas contienen persona, calidad, fecha y declaración. La fecha está comprendida entre el evento y hoy. El relato final es una reconstrucción independiente del relato inicial del caso.

El árbol se construye manualmente relacionando hechos mediante antecedente y consecuencia. Permite varios antecedentes y ramificaciones. Rechaza auto-relaciones, duplicados, ciclos y que el hecho final sea antecedente de otro. Las relaciones se pueden quitar antes del cierre. Su SVG incluye una alternativa tabular accesible.

Las entrevistas, hechos y medidas se pueden corregir mientras la investigación está en curso. Una medida de investigación no puede marcarse completada: primero se cierra la investigación y se inicia el plan. La activación del plan es transaccional, con índice único por propuesta y control de versión del expediente para evitar duplicaciones o escrituras simultáneas al cierre.

## Conservación de datos anteriores

Los casos que ya estaban en investigación reciben un expediente en curso con sus antecedentes reales disponibles. Las medidas anteriores se conservan como propuestas: el registro original no se borra, queda inactivo hasta iniciar el plan y se vincula a su propuesta. Deben vincularse a hechos antes del cierre.

Los casos que ya estaban en seguimiento o cerrados se identifican como registros históricos. Se conservan sus planes y evidencias, sin inventar entrevistas, hechos, fechas de inicio del plan, fechas de cierre de investigación ni relatos finales inexistentes. Su fecha de registro remite al antecedente original y no se presenta como una fecha de inicio de investigación acreditada. El sistema muestra esta condición explícitamente.

Dashboard y reportes de medidas incluyen únicamente acciones activas del plan; las propuestas todavía en investigación no alteran sus porcentajes de cumplimiento o atraso.

## Modelo agregado

Investigaciones tiene CasoId único, estado propio, antecedentes, relato final, fechas de inicio/cierre/inicio de plan y rowversion. Entrevistas, Hechos, RelacionesCausales y MedidasInvestigacion pertenecen a ella. Evidencias incorpora InvestigacionId. Medidas del plan incorpora Activa y PropuestaId único opcional para trazabilidad.

El cierre de la investigación deja el caso EnInvestigacion hasta que se inicia el plan, momento en que pasa a EnSeguimiento. Su cierre definitivo continúa exigiendo todas las medidas activas completadas. La IA sigue sin implementar y el árbol es enteramente manual.

## Correcciones del 3 de noviembre de 2026

- El cumplimiento actualiza únicamente la medida elegida, conservando los cumplimientos concurrentes de otras acciones y su auditoría. No cambia las rutas públicas ni requiere migraciones.
- Las siete secciones de investigación permanecen montadas al cambiar de pestaña. Los borradores de formularios, incluido el relato final, se conservan durante esa navegación. Deben guardarse antes de salir del expediente o recargar la página.
- La importación recupera el nombre del trabajador asociado cuando existe su registro y corrige los marcadores vacíos ya importados. La interfaz identifica ese nombre como referencia actual; no lo presenta como un nombre histórico acreditado. Los demás antecedentes y los nombres ya conservados no se sobrescriben. La corrección se ejecuta al iniciar el backend de desarrollo y es repetible sin duplicaciones.
- Verificación: 22 pruebas de backend aprobadas (una prueba de recuperación SQL omitida), compilación de producción correcta y cinco recorridos de navegador aprobados, incluido el ciclo investigación → cierre → plan → cumplimiento → cierre del caso y la conservación del relato entre pestañas. Evidencias: revision-mejoras.trx y frontend.json.
- Límite de verificación: el recorrido visual de administración no pudo completarse porque la cuenta local rechazó la contraseña de prueba guardada. No se restableció esa cuenta. Los controles de autorización de administración sí pasan en las pruebas aisladas. La consulta de vulnerabilidades de NuGet no estuvo disponible por conexión.
