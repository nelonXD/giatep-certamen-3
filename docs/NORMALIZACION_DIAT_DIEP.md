# Separación de los datos DIAT y DIEP

Actualización del 4 de noviembre de 2026.

Los datos se guardan de acuerdo con la entidad a la que pertenecen. El formulario que recibe y devuelve la API mantiene su contrato `denuncia: { formulario, datos }`; el servidor lo distribuye entre las tablas y lo reconstruye al consultar.

| Tabla | Datos que contiene | Relación |
| --- | --- | --- |
| Casos | Código, tipo, fecha del evento, relato, estado, fechas de registro/cierre, fecha de emisión, código de organismo y folio. También conserva los antiguos campos de denunciante y firma cuando existen. | TrabajadorId, EmpleadorId y EstablecimientoId |
| Trabajadores | RUN, nombre, cargo o profesión, dirección, comuna, teléfono, sexo, nacimiento, nacionalidad, pueblo originario, fecha de ingreso, antigüedad y unidad, tipo de contrato, tipo de ingreso y categoría ocupacional. | Un trabajador puede tener varios casos. |
| Empleadores | RUT, razón social, dirección, comuna, teléfono, actividad económica, trabajadores hombres/mujeres, propiedad, tipo de empresa y actividad principal. | Un empleador puede tener varios casos. |
| DIAT | Hora del accidente, ingreso y salida; dirección y comuna del accidente; actividad, lugar, trabajo habitual, realización habitual, clasificación, tipo de accidente, tipo de trayecto, medio y detalle de prueba. | CasoId único, FK a Casos.Id |
| DIEP | Duración y unidad de síntomas, molestias previas, parte del cuerpo, actividad y puesto al inicio de síntomas, compañeros con síntomas, agentes, tiempo y unidad de exposición. | CasoId único, FK a Casos.Id |

DIAT y DIEP no repiten los datos del trabajador, empleador ni encabezado. Un caso nuevo con formulario posee el detalle correspondiente a su tipo. Los casos históricos sin formulario permanecen sin detalle; no se inventan datos. Al cambiar el tipo de un borrador se reemplaza su detalle en la misma confirmación de cambios, conservando el caso y sus relaciones.

## Migración de la base existente

1. `CrearTablasDiatDiep` crea las tablas y agrega los campos compartidos a Casos y los laborales a Trabajadores.
2. `TrasladarDatosDiatDiep` archiva cada declaración anterior completa en Auditorias con acción `ArchivoNormalizacion`, copia los datos comunes a Casos y los exclusivos al detalle apropiado. Para los campos laborales nuevos se toma la declaración más reciente de cada trabajador. Los datos ya existentes en los maestros se conservan.
3. `SepararDiatDiep` retira la antigua tabla DenunciasCasos después de verificar los conteos y el contenido de cada detalle.

La columna DenunciaJson queda como compatibilidad de registros anteriores y permanece vacía en los casos migrados y las nuevas altas. No se utiliza como almacén duplicado. Las investigaciones conservan las instantáneas de antecedentes que ya tenían.

Los casos consultan los datos actuales del trabajador y del empleador relacionados. Actualizar un maestro se refleja en los casos que lo referencian. Las declaraciones originales anteriores al cambio pueden consultarse en la auditoría, y los antecedentes ya incorporados a una investigación permanecen en su instantánea.

La migración se ensayó sobre una copia restaurada de un respaldo COPY_ONLY con CHECKSUM antes de aplicarla a GiatepDB. La evidencia está en `evidencias/separacion-diat-diep.json`. Se conservaron 1.141 casos, 438 detalles DIAT y 194 DIEP; los casos restantes no tenían una declaración relacionada. El respaldo anterior permanece en el directorio de respaldos del servicio SQL Server indicado en esa evidencia.

La reversión operativa debe hacerse mediante un respaldo o una nueva migración que incluya los cambios posteriores. No ejecutar un downgrade automático: el archivo histórico de la migración conserva la situación anterior, no las altas y modificaciones posteriores.

## Revisión en SQL Server Management Studio

Actualizar la lista de tablas y agregar DIAT y DIEP al diagrama. Ambas deben mostrar su relación uno a uno opcional con Casos mediante CasoId, mientras Casos conserva sus referencias a Trabajadores y Empleadores. Eliminar del lienzo la representación antigua de DenunciasCasos si el diagrama guardado aún la muestra.
