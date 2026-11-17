> Modelo vigente del 4 de noviembre: Casos almacena los datos comunes del evento; Trabajadores y Empleadores almacenan sus propios datos; DIAT y DIEP contienen solo campos exclusivos con CasoId único. DenunciasCasos fue retirada. Detalle y evidencia en [Separación DIAT y DIEP](NORMALIZACION_DIAT_DIEP.md).

> Simplificación vigente: se retiró la sección de identificación del denunciante de DIAT y DIEP. No se exige nombre, RUN, teléfono, clasificación ni firma del denunciante al guardar. Los datos históricos se conservan y la auditoría identifica al usuario que registró el caso.

> Flujo vigente: al escribir desde dos dígitos en las casillas de RUT/RUN aparecen hasta ocho sugerencias con identificador y nombre, sin botón de búsqueda. Puedes elegir con el ratón o con flechas y Enter; Escape oculta la lista. Un RUT completo válido también completa automáticamente los datos encontrados. Si no existe, completa los datos y se registrarán al guardar el caso. Se propone el establecimiento del trabajador cuando está disponible.

# Registro DIAT y DIEP

Actualizado el 3 de noviembre de 2026. Formularios de referencia: achs_formulario-diat.pdf y achs_formulario-diep.pdf proporcionados por el usuario.

## Recorrido

1. Prevención pulsa Registrar caso. Un diálogo pregunta qué tipo de caso desea crear: Accidente del trabajo o Enfermedad profesional. Puede cancelar con el botón o Escape; el foco vuelve al botón de registro. Al elegir, se abre directamente el formulario DIAT o DIEP, sin selector de tipo dentro del formulario.
2. Escribe el RUT del empleador y RUN del trabajador y elige una sugerencia si existe; de lo contrario, completa sus datos para registrarlos al guardar. Indica el establecimiento y la fecha del evento/inicio de molestias. Los datos disponibles del empleador y trabajador se completan desde sus tablas. Al volver a registrar otro caso se vuelven a cargar los maestros para incluir los datos recién guardados. El establecimiento es el lugar de trabajo, distinto de la entidad empleadora.
3. Completa el formulario correspondiente: DIAT para accidente y DIEP para enfermedad. Ambos incluyen encabezado, empleador y trabajador. La identificación del denunciante no se solicita: la autoría del registro se conserva en la auditoría del usuario autenticado. El relato inicial se usa como descripción del accidente o de las molestias, según el caso.
4. Guarda el caso en borrador y revisa la denuncia en Ver datos de la denuncia DIAT/DIEP. Mientras no exista investigación puede editar sus antecedentes.
5. Desde el listado usa Iniciar investigación; tras iniciarla la misma fila permite Ver investigación. El botón solo está disponible para Prevención y para borradores sin investigación.
6. Los antecedentes de la denuncia se copian a la instantánea de investigación. El caso conserva su denuncia y queda bloqueada la edición de antecedentes.

## Datos y validación

La definición compartida `shared/denuncias.json` gobierna campos, etiquetas, opciones y condiciones en la interfaz y en el servidor. El backend incorpora esa definición en su compilación. Los datos se transportan en `denuncia: { formulario, datos }` y se guardan en columnas de las tablas relacionadas descritas a continuación, junto al tipo, fecha y relato ya existentes. El listado expone investigacionId. OpenAPI incluye el contrato actualizado.

| Sección | Contenido |
| --- | --- |
| Encabezado | Fecha de emisión; código y folio externos opcionales, sin inventar numeración del organismo administrador. |
| Empleador | Razón social, RUT, dirección, comuna, teléfono, actividad, dotación por sexo, propiedad y tipo de empresa; actividad de empresa principal cuando corresponde. |
| Trabajador | Nombre, RUN, domicilio, comuna, teléfono, sexo, nacimiento, edad calculada, nacionalidad, oficio, antigüedad/unidad, contrato, ingreso, categoría y pueblo originario. |
| DIAT | Fecha del evento, relato, hora del accidente/entrada/salida, dirección, comuna, actividad, área, trabajo habitual, clasificación, tipo trabajo/trayecto y antecedentes de prueba condicionados al trayecto. |
| DIEP | Fecha inicial, síntomas, duración/unidad, molestias previas, parte del cuerpo, actividad y puesto al inicio, compañeros con síntomas, agentes y tiempo/unidad de exposición. |
| Denunciante | Nombre, RUN, teléfono, clasificación y nombre de quien firmará. Las categorías siguen los roles simplificados del proyecto; no se crea un módulo ni rol de comité. |

El servidor valida tipo DIAT/DIEP, campos permitidos, obligatoriedad, dígito verificador de RUT/RUN, opciones cerradas, horas de 24 horas, números enteros no negativos y fechas. El nacimiento debe ser anterior o igual al evento y la emisión posterior o igual; no se permiten fechas futuras. Trayecto exige tipo, medio y detalle de prueba; esos campos se descartan al seleccionar Trabajo. Los antecedentes se validan también al editar. El nombre de quien firmará no es una firma electrónica. El registro interno no envía denuncias al organismo administrador.

Los expedientes anteriores y sus clientes siguen admitiendo ausencia de denuncia para compatibilidad; se muestran como registros sin DIAT/DIEP. No se inventan sus datos. Si un caso ya contiene denuncia, una edición debe conservarla. El formulario nuevo siempre la incorpora.

## Actualización y recuperación

La migración DenunciasDiatDiep agrega una columna nullable y conserva los registros anteriores. Antes de aplicarla se realizó respaldo COPY_ONLY con CHECKSUM y restauración de prueba en una base separada, sin sustituir GiatepDB. Evidencia: evidencias/respaldo-antes-denuncias.trx. La reversión de la migración elimina la columna y sus datos: antes de revertir, respaldar/exportar las denuncias nuevas. No se ejecutó esa reversión.

## Verificación final

31 pruebas de backend aprobadas; la recuperación SQL se verificó separadamente con una prueba aprobada antes de aplicar la migración. Seis recorridos de navegador aprobados: dashboard, DIAT con investigación/plan/cierre, reportes/exportaciones, Jefatura, teclado y DIEP con creación/edición/recarga/inicio de investigación. El formulario DIEP se comprobó con axe y en anchos 360, 768 y 1440 sin desbordamiento horizontal. Compilación de producción correcta. Se conserva la limitación anterior del recorrido visual del administrador por credenciales locales; no se modificó su cuenta.

La consulta de análisis ahora proyecta solo los datos necesarios para indicadores y reportes, sin cargar el JSON personal de las denuncias. En una comprobación local posterior al ajuste el dashboard respondió en aproximadamente 1,1 segundos; es una observación puntual, no una garantía de rendimiento. Las pantallas de detalle vuelven a consultar el caso al regresar de editarlo para mostrar sus cambios inmediatamente.


## Tablas relacionadas y selección previa

La estructura vigente distribuye los datos en Casos, Trabajadores, Empleadores, DIAT y DIEP. Los datos compartidos del evento se guardan en Casos, mientras que las personas y empresas se relacionan mediante sus identificadores. Cada detalle específico tiene CasoId único. La tabla mezclada DenunciasCasos fue retirada tras archivar las declaraciones originales y verificar su traslado.

Guardar confirma los datos del caso, trabajador, empleador y detalle en una sola operación transaccional. Las consultas usan los maestros actuales; las investigaciones conservan su instantánea independiente. Los antecedentes declarados antes de esta normalización se conservaron en Auditorias con acción ArchivoNormalizacion. Véase NORMALIZACION_DIAT_DIEP.md.

## Registro directo desde RUT/RUN

Los campos de RUT del empleador y RUN del trabajador se muestran primero en sus secciones. La búsqueda es exacta, valida el dígito verificador y admite formato con o sin puntos/guion. Las respuestas antiguas se cancelan al cambiar el RUT; cambiar de identidad limpia sus datos anteriores. Al editar un caso no se sobrescribe automáticamente su instantánea: la consulta se activa al editar el RUT o solicitar Buscar. Los resultados se anuncian y se puede buscar con Enter desde la misma casilla.

El alta no requiere trabajadorId. El servidor resuelve al trabajador por RUN y al empleador por RUT: reutiliza un registro existente o lo crea dentro del mismo guardado transaccional del caso. El trabajador nuevo recibe un código interno TRAB-RUN y el establecimiento elegido. No quedan maestros nuevos si el caso falla sus validaciones. Se mantienen los índices únicos. Los casos reutilizan el dato actual de cada maestro; las instantáneas de investigación anteriores permanecen sin cambios. Las cuentas antiguas que envían trabajadorId siguen admitidas, siempre que coincidan con la identidad y establecimiento indicados. Un trabajador inactivo o de otro establecimiento no se duplica para eludir esa condición.

Las búsquedas GET /api/v1/trabajadores/por-rut/{rut} y GET /api/v1/empleadores/por-rut/{rut} requieren Prevención. Un RUT inválido devuelve 400 y uno inexistente 404 con Problem Details; la interfaz distingue ausencia de un registro y un error de conexión. No se necesitan cambios de esquema para este flujo.

Validación: 36 pruebas de backend aprobadas (dos comprobaciones SQL locales omitidas en la ejecución normal); nueve recorridos de navegador aprobados, incluido un trabajador y empleador nuevos creados desde el caso y encontrados después mediante las mismas casillas de RUT. Compilaciones de backend e interfaz correctas. Evidencias: registro-por-rut.trx, frontend.json y caso-alta-directa-rut.png.


## Edad y antigüedad automáticas

La fecha de nacimiento completa calcula los años cumplidos a la fecha del evento o inicio de molestias. El mes y año de ingreso calcula la antigüedad en años y meses respecto de esa misma fecha; no se pide una cantidad ni unidad manual. El backend recalcula y almacena el total de meses, y conserva el mes de ingreso (AAAA-MM) en Trabajadores.FechaIngreso. No se aceptan ingresos posteriores al evento. Los expedientes anteriores sin mes de ingreso conservan la antigüedad declarada y se identifican como registro anterior en la edición, sin inventar fechas.
