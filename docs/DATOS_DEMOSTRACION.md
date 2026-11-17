# Datos de demostración para la rúbrica

El conjunto RUB26 aporta 600 casos nuevos y completos, distribuidos en 12 meses de historia hasta la fecha de su primera carga. Atiende el requisito explícito de la rúbrica: al menos 500 registros en la entidad principal, 12 meses y datos sintéticos verosímiles. La incorporación de estos datos no acredita por sí sola los demás criterios de la evaluación.

## Composición

| Entidad | Nuevos registros |
| --- | ---: |
| Casos | 600 |
| Accidentes con DIAT | 420 |
| Enfermedades con DIEP | 180 |
| Trabajadores distintos | 600 |
| Empleadores | 6 |
| Meses de historia | 12 |

Los seis empleadores llevan el prefijo Demo y tienen razón social, RUT de prueba, dirección ficticia, actividad, propiedad, tipo y dotación declarada. La dotación ficticia de cada empresa es de 500 personas; los trabajadores registrados representan a las personas de los casos del conjunto, no necesariamente a la totalidad de la dotación. Cada caso corresponde a un trabajador distinto para evitar una concentración inverosímil de cientos de incidentes en un grupo pequeño.

Los nombres se generan por combinaciones sintéticas y se identifican con Demo. Los RUT/RUN son valores de prueba con dígito verificador válido; no se obtuvieron de personas o empresas reales ni acreditan una identidad. Las direcciones se denominan Simulada/Ficticia y los teléfonos son ceros. No se utilizan datos personales reales.

## Coherencia del flujo

Hay borradores listos para iniciar investigación, investigaciones en curso, investigaciones cerradas esperando iniciar su plan, planes en seguimiento y casos cerrados. Los meses anteriores concentran más cierres; los recientes incluyen más trabajo en curso. Los estados varían dentro de cada mes y establecimiento.

Cada caso tiene su denuncia completa guardada en las tablas relacionadas. Las enfermedades distinguen inicio de molestias y fecha de denuncia, y la duración informada coincide con ese intervalo. Los horarios y etapas mantienen orden cronológico; los plazos de las medidas parten de la investigación o inicio de plan correspondiente, no del comienzo de síntomas. El ajuste de los plazos originales del conjunto RUB26 quedó auditado sin modificar registros de otros conjuntos. Las investigaciones cuentan con entrevista, acta de inspección PDF sintética, cuatro hechos, tres relaciones causales sin ciclos y dos medidas vinculadas a antecedentes. Las investigaciones cerradas incluyen relato final y una segunda entrevista. Solo las que iniciaron plan tienen medidas activas; los casos cerrados tienen todas sus medidas cumplidas.

Los escenarios incluyen resbalones, contacto con materiales, manejo de cargas, apertura de embalajes y accidentes de trayecto, además de molestias de muñeca, exposición a vapores y molestias cervicales. Los relatos, entrevistas, hechos y medidas se relacionan con el escenario correspondiente. Las medidas se asignan a Prevención o a una Jefatura del establecimiento correspondiente. Hay medidas cumplidas, pendientes en plazo y vencidas para demostrar los indicadores y las restricciones por rol.

Los meses contienen 62, 58, 54, 50, 48, 46, 44, 46, 50, 52, 48 y 42 casos, respectivamente. Estos son conteos de demostración, no tasas de accidentabilidad ni conclusiones sobre una institución real. El dashboard y los reportes calculan sus valores desde SQL Server y también incluyen los registros anteriores conservados.

## Cómo verlos

- Ingresar con Prevención y abrir Gestión de casos.
- Escribir **RUB26-** en Buscar para consultar exclusivamente los 600 casos nuevos; combinar con tipo, estado y establecimiento.
- Al registrar otro caso, elegir un empleador que comience por **Demo** y un trabajador de demostración. Sus datos se completan desde los maestros.
- Abrir los casos para revisar sus denuncias e investigaciones y continuar los flujos que estén pendientes.

## Reproducción y verificación

La implementación está en `backend/Giatep.Api/Services/DatosDemostracion.cs`. Se activa únicamente en la inicialización local de desarrollo mediante `Seed__DatosDemostracion=true`, documentado en `.env.example`. La carga comprueba su prefijo para no duplicarse y confirma todos los registros en una única transacción. Usa un contexto exclusivo de importación con inserciones individuales, lotes pequeños y una espera ampliada; la configuración de las operaciones normales no cambia. Los registros anteriores y las cuentas se conservan. El empleador técnico con nombre genérico de las pruebas anteriores se identifica como Pruebas previas, sin modificar sus denuncias históricas.

Los intentos de carga que excedieron la espera de SQL Server se revirtieron por completo antes de usar el contexto de inserciones individuales. Se confirmó que no dejó casos RUB26 parciales. El respaldo previo se verificó mediante restauración en otra base, sin reemplazar GiatepDB.

Las pruebas comprueban cantidades, meses, formularios, identidades de prueba válidas, relaciones, estados, fechas, medidas y repetición sin duplicados. La comprobación local de SQL Server genera `evidencias/datos-rubrica.json` con los conteos reales y la distribución de estados. Los ensayos de navegador verifican búsqueda de los 600 registros, los 180 DIEP y la consulta de sus antecedentes.


## Evidencia registrada

La comprobación de SQL Server confirmó 600 denuncias, 517 investigaciones (54 en curso y 23 cerradas esperando plan), 440 planes, 980 entrevistas, 517 actas PDF de demostración, 2.068 hechos, 1.551 relaciones causales y 1.034 propuestas. Los 600 casos incluyen 380 cerrados, 83 borradores, 77 en investigación y 60 en seguimiento. El detalle de medidas y fecha de comprobación está en evidencias/datos-rubrica.json.

El aumento de volumen permitió detectar una consulta pesada de análisis. Se separó la lectura de casos y medidas activas para evitar multiplicar y ordenar filas anchas. Una comprobación local del dashboard respondió en aproximadamente 0,8 segundos después del ajuste; es una medición puntual, no una garantía general de rendimiento. El importador de antecedentes también consulta primero los identificadores pendientes y evita cargar relaciones cuando no hay casos por importar.


Los catálogos desplegables se ordenan después de leer sus datos y el análisis evita ordenaciones anchas en SQL Server. Esto elimina las grandes reservas de memoria que se observaron al abrir la página con el volumen ampliado. Los listados principales mantienen filtros y paginación en SQL. La comprobación final del backend aprobó 35 pruebas, incluida la lectura del conjunto cargado en SQL Server; la prueba de respaldo ya se había ejecutado separadamente antes de importar.

Verificación final en navegador: ocho recorridos aprobados, incluidos creación DIAT, creación/edición DIEP, investigación y plan, dashboard responsive, reportes/exportaciones, Jefatura, teclado, diálogo de selección y búsqueda del conjunto RUB26. La navegación cancela lecturas abandonadas y la coordinación del caso consulta observaciones mediante una ruta ligera con autorización por ámbito; no descarga un historial completo que no se muestra en esa pantalla. La compilación de la interfaz también fue correcta. La prueba visual del administrador conserva la limitación de credenciales documentada previamente y no integra estos ocho recorridos.
