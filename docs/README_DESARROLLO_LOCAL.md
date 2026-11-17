> Actualización del 8 de noviembre de 2026: se implementó OCR local con un modelo neuronal preentrenado de Tesseract en español. En Evidencias de investigaciones abiertas permite reconocer una imagen PNG/JPG, revisar el texto y crear un Word asociado a la fuente. No usa OpenAI, no detecta objetos ni genera conclusiones. La IA generativa o detección de objetos mencionada como pendiente en versiones anteriores no forma parte de este OCR. Consulta el README para instalación y límites.

> Modelo actualizado: Casos contiene los datos comunes; Trabajadores y Empleadores conservan sus datos; DIAT y DIEP guardan únicamente sus datos exclusivos. [Separación de formularios](docs/NORMALIZACION_DIAT_DIEP.md).

> Plan de acción vigente: menú propio, entrega con evidencia, aprobación o devolución por Prevención y cierre cuando todas las medidas estén verificadas. [Guía de uso](docs/PLANES_ACCION.md).

> Flujo vigente: al escribir desde dos dígitos en las casillas de RUT/RUN aparecen hasta ocho sugerencias con identificador y nombre, sin botón de búsqueda. Puedes elegir con el ratón o con flechas y Enter; Escape oculta la lista. Un RUT completo válido también completa automáticamente los datos encontrados. Si no existe, completa los datos y se registrarán al guardar el caso. Se propone el establecimiento del trabajador cuando está disponible.

> Nuevos datos de demostración: 600 casos completos RUB26 en 12 meses, 600 trabajadores y seis empleadores. Se cargan una sola vez en desarrollo con `Seed__DatosDemostracion=true`. Consulta [Datos de demostración](docs/DATOS_DEMOSTRACION.md).

> Actualización del 3 de noviembre: el registro de casos permite seleccionar accidente (DIAT) o enfermedad profesional (DIEP), completar los datos de su denuncia e iniciar la investigación desde el listado. Consulta [Registro DIAT y DIEP](docs/DENUNCIAS.md).

# GIATEP

Sistema académico de gestión e investigación de accidentes del trabajo y enfermedades profesionales. Proyecto independiente del taller mecánico, con React + TypeScript, ASP.NET Core y SQL Server.

## Alcance

**Actualización del 2 de noviembre:** la investigación tiene módulo propio y se inicia desde el caso. Incluye antecedentes, entrevistas, evidencias, relato final, hechos, árbol de causas manual y medidas. Solo después de cerrarla se inicia el plan de acción con sus propuestas. Consulta docs/INVESTIGACIONES.md para el flujo y la conservación de registros anteriores.

Login JWT con tres roles, establecimientos, trabajadores, expediente de casos, causas, evidencias, observaciones, medidas con responsable y plazo, cierre controlado y auditoría. Dashboard con seis indicadores y cuatro visualizaciones filtrables. Tres reportes con XLSX, PDF, CSV e impresión. Datos iniciales: 500 casos sintéticos distribuidos en doce meses.

**La integración de IA queda pendiente por solicitud del usuario. Docker se omite por indicación del profesor comunicada por el usuario. No hay carpeta scripts.**

## Iniciar en este equipo

Primera terminal, desde la carpeta del proyecto:

```powershell
cd backend/Giatep.Api
dotnet run
```

Segunda terminal, desde la carpeta del proyecto:

```powershell
cd frontend
npm ci
npm run dev
```

Después de instalar dependencias, basta con `npm run dev`. Abre la dirección indicada por Vite, normalmente http://127.0.0.1:5173. Si ese puerto está ocupado, usa el que indique la terminal. Mantén SQL Server y ambas terminales iniciados. Ctrl+C detiene cada servicio.

La API se inicia en http://127.0.0.1:5080. Sus opciones locales de puerto y Development están guardadas en Properties/launchSettings.json. La API lee el archivo privado backend/.env, aplica migraciones al iniciar en desarrollo y conserva los registros existentes.

## Cuentas

Las cuentas locales son `admin`, `prevencion`, `jefatura` y `jefatura.sur`. Sus contraseñas generadas se encuentran en **CREDENCIALES.local.md**, privado y excluido de Git. Las dos jefaturas pertenecen a establecimientos distintos. Puedes cambiar la contraseña desde la barra superior o administrar cuentas desde el rol Administrador.

No uses las credenciales SQL para ingresar a la web. SQL y los usuarios de la aplicación son cuentas diferentes.

## Instalación nueva

Requisitos: SDK .NET 10, Node 22.12 o posterior compatible con Vite 8 y SQL Server accesible. Copia backend/.env.example a backend/.env y define conexión, clave JWT aleatoria de al menos 32 bytes y las cuatro contraseñas iniciales de al menos 12 caracteres. No subas esos valores al repositorio. Development crea datos de demostración solo si no existen establecimientos. En producción no se ejecuta ese sembrado ni la migración automática.

## Roles

| Rol | Permisos |
| --- | --- |
| Administrador | Usuarios, centros, trabajadores, auditoría, dashboard y reportes; no opera investigaciones |
| Prevención | Casos de todos los centros, investigación, causas, evidencias, medidas, cumplimiento y cierre |
| Jefatura | Casos, observaciones, análisis y reportes de su centro; cumplimiento de medidas asignadas a su cuenta |

El backend verifica rol y establecimiento. Ocultar botones no sustituye esa comprobación.

## Verificación

Desde la raíz:

```powershell
dotnet test backend/Giatep.Tests
```

Desde frontend:

```powershell
npm run build
npx playwright install chromium
npm test
```

Las pruebas de navegador requieren API y frontend funcionando, .env local y GIATEP_WEB_URL si usas un puerto diferente al configurado para revisión (5173):

```powershell
$env:GIATEP_WEB_URL = 'http://127.0.0.1:5173'
npm test
```

Las pruebas crean únicamente datos sintéticos. Los resultados, auditorías y ejemplos de exportación están en evidencias. La prueba de recuperación SQL se habilita por separado con GIATEP_SQL_RECOVERY=1 y GIATEP_ROOT; crea un respaldo y una copia restaurada independiente sin reemplazar GiatepDB. Consulta PLAN_DESPLIEGUE.

## API y documentación

Contrato exportado: docs/openapi.json. Documentación interactiva en desarrollo: http://127.0.0.1:5080/scalar/v1. Salud: /health. Rutas de aplicación: /api/v1/auth, /casos, /opciones, /dashboard, /reportes y /admin. El contrato documenta JWT. Errores de negocio y validación usan Problem Details.

- docs/INFORME_TECNICO.pdf: informe de integración.
- docs/PRESENTACION.pdf: material de defensa, con fuente Markdown.
- docs/MANUAL_USUARIO.md: instrucciones por rol.
- docs/REQUERIMIENTOS.md: historias, aceptación y trazabilidad académica.
- docs/PLAN_DESPLIEGUE.md: diez apartados sin Docker.
- docs/DICCIONARIO_DATOS.md y docs/diagramas: modelo, restricciones y fuentes/exportaciones.
- docs/GUIA_ESTILOS.md y docs/SEGURIDAD.md: diseño y controles.
- docs/VALIDACION.md: evidencias, resultados y verificaciones humanas pendientes.

## Entrega continua

.github/workflows/ci.yml compila, prueba y publica un artefacto de API, web y documentación solo si pasa la validación. Es una configuración lista para ejecutarse en GitHub; su ejecución remota depende de publicar el repositorio. No incluye secretos ni utiliza Docker.

## Límites y preparación de entrega

La IA no está implementada. Aprobación del dominio, integrantes, aportes reales y defensa se acreditan con el docente y el equipo. La auditoría automática de accesibilidad no sustituye una prueba humana con lector de pantalla. El plan institucional es una propuesta, no un despliegue contratado. No incluir .env, CREDENCIALES.local.md ni respaldos en la entrega.

## Apoyo con IA

Se utilizó Codex para apoyar implementación, documentación y pruebas. El equipo debe revisar y poder explicar/modificar lo entregado. Ese apoyo de desarrollo es distinto de la integración externa de IA del producto, que permanece pendiente. Consulta AI_LOG.md.
