# Planes de acción

El menú Planes de acción está disponible para Prevención y Jefatura. Reúne investigaciones cerradas pendientes de iniciar el plan, planes en seguimiento y casos cerrados. Se puede buscar por código o establecimiento y filtrar por estado. La lista muestra medidas verificadas, pendientes de revisión y vencidas.

## Recorrido

1. Cerrar la investigación completa. Desde ella, pulsar Iniciar plan de acción, o abrir su ficha en Planes de acción e iniciarlo allí. Las medidas, hechos causales, responsables y plazos se transfieren desde la investigación una sola vez.
2. Abrir la medida asignada y registrar el detalle de implementación y una evidencia PDF, PNG o JPG de hasta 5 MB. Enviar a verificación no completa la medida.
3. Prevención revisa la evidencia, escribe su observación y aprueba o devuelve la entrega. Al devolverla, el responsable puede presentar una nueva implementación con nueva evidencia. Se conservan los archivos anteriores y la auditoría de los cambios.
4. Una aprobación marca la medida como verificada. El avance del plan cuenta únicamente estas medidas.
5. Cuando todas están verificadas, Prevención puede cerrar el caso y su plan. El expediente queda de solo lectura.

Las medidas se filtran por asignación propia, estado o plazo vencido. Una medida pendiente de revisión también puede estar vencida. Los plazos son los definidos en la investigación; el plan no modifica retrospectivamente sus propuestas.

## Permisos y conservación

Jefatura consulta solo su establecimiento y entrega únicamente sus propias asignaciones. Prevención gestiona las entregas y es el único rol que aprueba, devuelve y cierra. Administración no opera planes. Las descargas exigen acceso al caso y quedan auditadas.

Se preservan los cumplimientos anteriores a este módulo, identificados sin inventar evidencias o revisiones. Las entregas nuevas requieren evidencia y revisión. El antiguo endpoint de cumplimiento directo devuelve conflicto y remite al nuevo flujo.

## Almacenamiento

Medidas agrega ImplementadaEn, ComentarioCumplimiento, RevisadaEn, RevisorId y Revision. EvidenciasMedidas almacena archivos, fecha, usuario y relación con la medida; es independiente de las evidencias de la investigación. La migración SeguimientoPlanAccion es aditiva y no modifica los registros existentes.

API: GET /api/v1/planes, GET /api/v1/planes/{casoId}, POST /api/v1/planes/{casoId}/medidas/{id}/entrega (formulario con comentario y archivo), PUT /api/v1/planes/{casoId}/medidas/{id}/revision (aprobada y observacion), GET /api/v1/planes/{casoId}/evidencias/{id}. El inicio y cierre utilizan los endpoints existentes de investigación y caso.
