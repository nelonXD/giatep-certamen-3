> Plan de acción vigente: menú propio, entrega con evidencia, aprobación o devolución por Prevención y cierre cuando todas las medidas estén verificadas. [Guía de uso](PLANES_ACCION.md).

> Flujo vigente: al escribir desde dos dígitos en las casillas de RUT/RUN aparecen hasta ocho sugerencias con identificador y nombre, sin botón de búsqueda. Puedes elegir con el ratón o con flechas y Enter; Escape oculta la lista. Un RUT completo válido también completa automáticamente los datos encontrados. Si no existe, completa los datos y se registrarán al guardar el caso. Se propone el establecimiento del trabajador cuando está disponible.

> Actualización: Trabajadores, Empleadores, Casos y DenunciasCasos almacenan datos en tablas relacionadas. Registrar caso abre un diálogo previo de selección, sin selector de tipo dentro del formulario. Detalles vigentes en [Registro DIAT y DIEP](DENUNCIAS.md).

> Actualización del 3 de noviembre: el registro de casos permite seleccionar accidente (DIAT) o enfermedad profesional (DIEP), completar los datos de su denuncia e iniciar la investigación desde el listado. Consulta [Registro DIAT y DIEP](DENUNCIAS.md).

> Actualización vigente del 2 de noviembre de 2026: la investigación es independiente del caso, con entrevistas, evidencia, relato final, hechos, árbol y medidas. El plan se inicia después de cerrar la investigación. El detalle y las reglas que reemplazan el flujo anterior están en INVESTIGACIONES.md.

# Manual de usuario de GIATEP

## Ingresar

Abre la dirección que muestre Vite, normalmente http://127.0.0.1:5173. Ingresa cuenta y contraseña. Las cuentas iniciales de este equipo se encuentran en el archivo privado `CREDENCIALES.local.md`; no debe adjuntarse a la entrega. En otra instalación, el administrador define las contraseñas iniciales en .env antes del primer inicio.

Las cuentas iniciales son admin, prevencion, jefatura y jefatura.sur. Las dos jefaturas pertenecen a centros distintos para demostrar el aislamiento. Las contraseñas se generan localmente y se almacenan como hash en SQL. Cinco intentos fallidos bloquean temporalmente una cuenta durante diez minutos. La sesión expira a los treinta minutos.

## Prevención de Riesgos

1. En Gestión de casos, registra el evento con establecimiento, trabajador, fecha y relato inicial.
2. Abre el caso y pulsa Iniciar investigación. Se crea un expediente independiente y se conservan los antecedentes del evento.
3. En el menú Investigaciones abre ese expediente. Completa Entrevistas (persona, calidad, fecha y declaración) y adjunta Evidencias en PDF, PNG o JPG de hasta 5 MB.
4. Escribe y guarda el Relato final de lo ocurrido, basado en los antecedentes y testimonios.
5. En Lista de hechos agrega los antecedentes verificados y un único hecho final. En Árbol de causas relaciona cada antecedente con su consecuencia hasta llegar al hecho final. El sistema impide ciclos.
6. En Medidas propone acciones vinculadas a hechos antecedentes, con responsable y plazo. Aún no forman un plan activo y no se pueden marcar como cumplidas.
7. Pulsa Cerrar investigación. Se exige entrevista, evidencia, relato final, árbol completo y medidas vinculadas. La investigación queda de solo lectura, pero el caso sigue abierto.
8. Pulsa Iniciar plan de acción. Se activan sus medidas una única vez y se vuelve al caso para realizar seguimiento.
9. Registra cumplimiento de las medidas. Cuando todas estén completadas, pulsa Cerrar caso.

Las entrevistas, hechos y medidas pueden editarse antes del cierre de la investigación. Las relaciones causales pueden quitarse para corregir el árbol. Los registros antiguos se señalan explícitamente y no se completaron con entrevistas o hechos inventados.

## Jefatura

Consulta Gestión de casos: solo aparecen registros de tu establecimiento. Abre el expediente y deja observaciones si sigue abierto. En Plan de acción, el botón Registrar cumplimiento aparece únicamente en las medidas asignadas a tu cuenta y cuando el caso está en seguimiento. Confirmar el cumplimiento declara que la implementación fue verificada; no sustituye una evidencia institucional.

Dashboard y reportes incluyen únicamente tu ámbito autorizado, incluso al cambiar filtros o solicitar una exportación directamente a la API. Jefatura no crea ni cierra casos y no accede a administración.

## Administrador

En Administración puedes agregar y editar usuarios, establecimientos y trabajadores. Una jefatura requiere establecimiento activo. Las contraseñas nuevas deben tener al menos 12 caracteres. Al editar una cuenta, deja la contraseña vacía si deseas conservarla.

Para una baja, elige Inactivo y confirma. El sistema conserva el historial. No puedes desactivar tu propia cuenta ni retirarte el rol administrador. Las cuentas con medidas pendientes deben resolverlas antes de cambiar su rol, centro o estado. Los establecimientos con usuarios activos tampoco pueden desactivarse.

La pestaña Auditoría muestra actor, fecha, entidad y acción. Expande Ver cambios para consultar los campos registrados. El administrador puede consultar dashboard y reportes; no opera investigaciones.

## Dashboard

Selecciona fechas de los eventos y establecimiento. Se muestran seis indicadores con fórmula y propósito, una serie mensual, composición por tipo, volumen por centro y avance por estado. Cada gráfico permite abrir su tabla. Los conteos no equivalen a tasas de riesgo: no se dispone de horas trabajadas ni población expuesta por período.

El panel se actualiza cada minuto y al recuperar foco. Usa Actualizar para consultar inmediatamente. Si no hay datos, amplía el rango: un cero no prueba ausencia real de riesgo.

## Reportes y archivos

Selecciona Operativo, Gerencial o Parametrizable. El último lista las medidas de los casos ocurridos entre las fechas elegidas. Cambiar un filtro vuelve a la primera página.

XLSX descarga una hoja con encabezados, números y fechas tipadas. PDF incluye título, emisión, filtros y páginas con texto seleccionable. CSV usa UTF-8 con BOM y punto y coma. Las exportaciones incluyen todos los registros filtrados, aunque la pantalla esté paginada. Imprimir todo prepara la colección completa y oculta navegación y botones.

## Teclado y errores

Tab y Mayús+Tab recorren controles; Enter activa enlaces y botones; Espacio activa botones y abre detalles. El enlace Saltar al contenido aparece al recibir foco. La navegación móvil se abre con Mostrar navegación. Los errores de campos se muestran en texto al salir del campo y los errores de operación se anuncian como alertas.

Si la sesión expira, vuelve a ingresar. Si un cambio de estado se bloquea, revisa causas, medidas y permisos. Si no hay conexión, comprueba que la API y SQL Server estén iniciados. Para cambiar tu contraseña usa Contraseña en la barra superior; el sistema pedirá ingresar nuevamente.
