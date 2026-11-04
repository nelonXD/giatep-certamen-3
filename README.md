# GIATEP

Sistema de gestión e investigación de accidentes del trabajo y enfermedades profesionales. Incluye registro DIAT/DIEP, investigaciones, planes de acción, evidencias y reportes, con acceso por roles.

## Requisitos

- SDK de .NET 10.
- Node.js 24 y npm.
- SQL Server disponible y una cuenta con permisos para crear y actualizar la base de datos local.

## Configuración local

Desde la carpeta del proyecto, copia `.env.example` como `.env`. Configura la conexión a SQL Server, una clave JWT aleatoria de al menos 32 bytes y las cuatro contraseñas iniciales de al menos 12 caracteres. Guarda estos valores únicamente en tu equipo.

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

- `backend`: API ASP.NET Core, modelo, migraciones de Entity Framework Core y pruebas.
- `frontend`: interfaz React con TypeScript y Vite, y pruebas de interfaz.
- `shared`: definición compartida de los formularios DIAT y DIEP, utilizada por la API.
- `.github/workflows`: validación y generación de artefactos.

El repositorio no incluye credenciales, bases de datos, dependencias instaladas, compilaciones, informes académicos ni evidencias locales.
