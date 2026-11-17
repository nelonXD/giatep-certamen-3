> Modelo vigente del 4 de noviembre: Casos almacena los datos comunes del evento; Trabajadores y Empleadores almacenan sus propios datos; DIAT y DIEP contienen solo campos exclusivos con CasoId único. DenunciasCasos fue retirada. Detalle y evidencia en [Separación DIAT y DIEP](NORMALIZACION_DIAT_DIEP.md).

> Actualización: Trabajadores, Empleadores, Casos, DIAT y DIEP almacenan datos en tablas relacionadas. Registrar caso abre un diálogo previo de selección, sin selector de tipo dentro del formulario. Detalles vigentes en [Registro DIAT y DIEP](DENUNCIAS.md).

> Actualización vigente del 2 de noviembre de 2026: la investigación es independiente del caso, con entrevistas, evidencia, relato final, hechos, árbol y medidas. El plan se inicia después de cerrar la investigación. El detalle y las reglas que reemplazan el flujo anterior están en INVESTIGACIONES.md.

# Modelo y diccionario de datos

Las tablas operativas usan identificadores GUID, salvo Auditorias que usa bigint autoincremental. Las longitudes y restricciones definitivas están en GiatepDbContext y las migraciones versionadas. Los campos no indicados como opcionales son obligatorios.

| Tabla | Campos principales | Relaciones e integridad |
| --- | --- | --- |
| Establecimientos | Id, Nombre nvarchar(120), Direccion nvarchar(200), Activo bit | Nombre único; baja lógica |
| Trabajadores | Id, Codigo nvarchar(30), Nombre nvarchar(120), Cargo nvarchar(100), EstablecimientoId, Activo | Código único; FK a establecimiento con borrado restringido |
| Usuarios | Id, Cuenta nvarchar(50), Nombre nvarchar(120), PasswordHash nvarchar(500), Rol nvarchar(20), EstablecimientoId opcional, Activo, Fallos, BloqueadoHasta opcional | Cuenta única; rol entre Administrador/Prevencion/Jefatura; Jefatura exige centro; contraseña hash PBKDF2 de Identity |
| Casos | Id, Codigo nvarchar(32), Tipo int, Establecimiento nvarchar(120), Fecha date, Relato nvarchar(max) limitado a 5.000 en modelo/API, Estado int, CreadoEn datetimeoffset, EstablecimientoId opcional, TrabajadorId opcional, CerradoEn opcional | Código único; tipo 0/1; estados 0-3; longitud mínima de relato 20 y centro 3; FK a centro y trabajador |
| Medidas | Id, CasoId, Descripcion nvarchar(1000), Responsable nvarchar(120), Plazo date, Completada bit, UsuarioResponsableId opcional | FK a caso y responsable; mínimo 10 de descripción y 3 de responsable |
| Causas | Id, CasoId, Categoria nvarchar(80), Descripcion nvarchar(1000) | FK a caso; categoría controlada por API |
| Observaciones | Id, CasoId, UsuarioId, Autor nvarchar(120), Texto nvarchar(2000), Fecha datetimeoffset | FK a caso y usuario; autor histórico conservado |
| Evidencias | Id, CasoId, UsuarioId, Nombre nvarchar(180), Tipo nvarchar(80), Contenido varbinary(max), Fecha | FK a caso y usuario; límite 5 MB, extensión y firma validados en API |
| Auditorias | Id bigint, UsuarioId opcional, Actor nvarchar(120), Entidad nvarchar(80), RegistroId nvarchar(80), Accion nvarchar(40), Cambios nvarchar(max), Fecha | Sin FK a usuario para conservar trazabilidad histórica; no guarda password ni binarios |
| __EFMigrationsHistory | MigrationId, ProductVersion | Historial de esquema administrado por EF Core |

## Normalización y conservación histórica

Centros, trabajadores, usuarios, casos, medidas, causas, observaciones y evidencias se mantienen en entidades separadas. Las relaciones de uno a muchos evitan listas en columnas y repetición de grupos. Casos.Establecimiento, Medidas.Responsable y Observaciones.Autor son instantáneas descriptivas deliberadas: preservan el nombre observado al registrar el hecho aunque el maestro cambie después. Los GUID mantienen la relación actual. Esta desnormalización se documenta para no confundir una instantánea histórica con el maestro.

Los FK opcionales de casos y responsables permiten conservar registros de la primera versión. La API exige identificadores válidos para nuevas altas. No se permite borrar casos por API. Activo evita romper la historia de entidades maestras.

## Índices y consultas

Índices únicos: código de caso, código de trabajador, cuenta de usuario y nombre de centro. Índice compuesto Estado/Fecha para consultas operativas, Fecha de auditoría para revisión reciente y FK de relaciones creados por EF. Los listados usan Where, OrderBy, Skip y Take en SQL, y cargan relaciones de medidas cuando se requiere detalle o agregación.

Los endpoints /dashboard y /reportes usan consultas LINQ preparadas con los mismos filtros y alcance por centro. Las agregaciones finales se materializan para el volumen académico de 500 casos; se mantiene el límite de exportación de 20.000 filas. La ampliación a volúmenes institucionales requiere medición, índices adicionales y agregaciones completas en SQL.

## Recuperación

El procedimiento probado está en SqlRecoveryTests. Los resultados de CHECKSUM y comparación de conteos se guardan en evidencias/restauracion.json. La copia restaurada se elimina al terminar, el respaldo permanece en el directorio del servicio SQL y la base original no se reemplaza.

## Vistas versionadas

v_CasosOperativos expone detalle y cantidad de medidas; v_ResumenEstablecimiento agrupa por centro y mes; v_SeguimientoMedidas relaciona casos y acciones. La migración VistasReportes las crea junto con comprobaciones adicionales de fechas, longitudes, categorías y tamaño de evidencias. Los endpoints usan consultas LINQ equivalentes con autorización por centro.


Actualización: Trabajadores.FechaIngreso es un texto opcional AAAA-MM. Antiguedad almacena el total de meses calculado a la fecha del caso y AntiguedadUnidad queda en Meses cuando se registra FechaIngreso. Los datos históricos sin FechaIngreso mantienen su valor y unidad originales.
