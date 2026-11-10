# GIATEP

Sistema de gestión e investigación de accidentes del trabajo y enfermedades profesionales. Incluye registro DIAT/DIEP, investigaciones, planes de acción, evidencias y reportes, con acceso por roles.

## Requisitos

- SDK de .NET 10.
- Node.js 24 y npm.
- SQL Server disponible y una cuenta con permisos para crear y actualizar la base de datos local.

## Reconocimiento de evidencias mediante API externa

El proveedor predeterminado es OCR.space. Consulta [configuración, costos y privacidad](backend/ocr/API.md). Configura Ocr__Engine=OcrSpace y Ocr__ApiKey en backend/.env; el OCR necesita internet. Paddle y Tesseract quedan como alternativas locales opcionales.

El flujo permite subir PNG/JPG de hasta 1 MB, revisar el texto y confirmar un Word. Las imágenes se envían al proveedor externo. Para la evaluación usa únicamente documentos sintéticos. La imagen y el Word revisado se guardan juntos al confirmar; el Word identifica el original por nombre e ID. Antes de confirmar, el escaneo no guarda evidencias.

## Configuración local

Desde la carpeta del proyecto, copia `backend/.env.example` como `backend/.env`. Configura la conexión a SQL Server, una clave JWT aleatoria de al menos 32 bytes y las cuatro contraseñas iniciales de al menos 12 caracteres. Guarda estos valores únicamente en tu equipo.

En desarrollo, la API lee este archivo, aplica las migraciones e inicializa los datos necesarios. `Seed__DatosDemostracion=true` permite cargar datos ficticios de demostración. Los usuarios iniciales y sus roles se definen en el inicializador de la API; sus contraseñas se toman de las variables `Seed__*Password`.

## Ejecutar

En una terminal:

```powershell
cd backend/Giatep.Api
dotnet run
```

En otra terminal, desde la carpeta del proyecto:

```powershell
cd frontend
npm ci
npm run dev
```

Abre la dirección que muestre Vite, normalmente http://127.0.0.1:5173. La API local usa http://127.0.0.1:5080. Mantén SQL Server y ambos servicios iniciados.

## Verificación

Desde la carpeta del proyecto:

```powershell
dotnet test backend/Giatep.Tests
```

Desde `frontend`:

```powershell
npm run build
```

Las pruebas de interfaz requieren los servicios en ejecución, la configuración local y los navegadores de Playwright instalados (`npx playwright install chromium`). Se ejecutan con `npm test`. Las pruebas especiales de SQL requieren configuración adicional y están desactivadas de forma predeterminada.

## Estructura

- `backend`: solución `Giatep.slnx`, configuración local `.env` y plantilla `.env.example`, API ASP.NET Core, modelo, migraciones de Entity Framework Core y pruebas.
- `frontend`: interfaz React con TypeScript y Vite, y pruebas de interfaz.
- `shared`: definición compartida de los formularios DIAT y DIEP, utilizada por la API.
- `.github/workflows`: validación y generación de artefactos.

El repositorio no incluye credenciales, bases de datos, dependencias instaladas, compilaciones, informes académicos ni evidencias locales.

## Archivos de evidencia

Las evidencias de investigaciones y de cumplimiento de planes se guardan en `backend/storage/evidencias`, fuera de las carpetas públicas y de Git. SQL Server conserva ruta relativa, nombre original, tipo, tamaño y SHA-256. Las descargas solo se entregan mediante la API tras validar permisos; la lectura comprueba la integridad del archivo.

En desarrollo, al iniciar la nueva versión se exportan y verifican las evidencias anteriores antes de aplicar la migración que retira el contenido binario de SQL. El traslado se detiene ante diferencias de integridad. Para un despliegue administrado, con la conexión y `Storage__Path` configurados, ejecuta la API con `--migrar-archivos=true` antes de iniciar el servicio. La migración SQL directa se bloquea si existen archivos sin verificación previa.

Los respaldos deben incluir SQL Server y el directorio de almacenamiento del mismo momento. Para volver al esquema anterior, restaura ambos respaldos y la versión de la aplicación correspondiente; la migración no intenta reconstruir archivos desde SQL. `Storage__Path` permite usar otra carpeta privada del servidor. No elimines ni renombres archivos manualmente.

El flujo OCR mantiene las imágenes nuevas como temporales y muestra una vista previa antes de confirmar el Word. La vista previa no crea una evidencia.
