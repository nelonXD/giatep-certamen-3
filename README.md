# GIATEP — Gestión e Investigación de Accidentes del Trabajo y Enfermedades Profesionales

Sistema integral para la formalización de denuncias (DIAT y DIEP), investigación causal mediante Árbol de Causas (Grafo Acíclico Dirigido), formulación y seguimiento de planes de acción con evidencias, analítica preventiva de Seguridad y Salud Ocupacional (S&SO), reportes multi-formato y digitalización asistida por IA/OCR.

Desarrollado para el **Certamen 3 (Hito 3)** de **Lenguaje de Programación Web II** (Integración: POO II, Bases de Datos, Inteligencia de Negocios I y II).

---

## Despliegue en la Nube (Producción Activa)

- **Frontend SPA (Vercel Edge Network)**: [https://giatep.vercel.app](https://giatep.vercel.app)
- **Backend API (Render Web Service con Docker Multi-Stage)**: [https://giatep-api.onrender.com](https://giatep-api.onrender.com)
- **Endpoint de Salud**: `GET https://giatep-api.onrender.com/health` (reporta estado y conexión a Supabase PostgreSQL 17).

---

## Ejecución Rápida con un Único Comando (Docker Compose)

El sistema está completamente contenerizado y listo para arrancar en cualquier máquina con Docker Desktop:

1. Clona el repositorio y sitúate en la raíz del proyecto.
2. Copia la plantilla de variables de entorno:
   ```bash
   cp .env.example .env
   ```
3. Inicia todo el stack (Base de datos PostgreSQL 17, Backend .NET 10 y Frontend React 19 / Nginx):
   ```bash
   docker compose up --build
   ```
4. Accede en tu navegador:
   - **Frontend Web**: [http://localhost:3000](http://localhost:3000)
   - **Backend API**: [http://localhost:5080/health](http://localhost:5080/health)

---

## Ejecución en Desarrollo Local (Sin Docker)

### Requisitos Previos
- SDK de **.NET 10**.
- **Node.js 24** y npm.
- **PostgreSQL 17** (o Supabase) o **SQL Server Express**.

### Pasos
1. Configura el archivo `backend/.env` copiando `backend/.env.example`.
2. En una terminal, inicia la API:
   ```powershell
   cd backend/Giatep.Api
   dotnet run
   ```
3. En otra terminal, inicia el frontend con Vite:
   ```powershell
   cd frontend
   npm ci
   npm run dev
   ```
4. Abre la URL local de Vite (típicamente `http://127.0.0.1:5173`). La API local responderá en `http://127.0.0.1:5080`.

---

## Cuentas de demostración y roles

El sistema incluye 600 casos sintéticos completos (conjunto RUB26) y 3 roles diferenciados:

| Rol | Usuario | Ámbito / Permisos |
|---|---|---|
| **Administrador** | `admin` | Gestión global de usuarios, centros y auditorías. |
| **Prevención** | `prevencion` | Registro DIAT/DIEP, investigaciones, árboles causales y aprobación de planes. |
| **Jefatura (Centro Norte)** | `jefatura` | Consulta de casos, investigaciones y planes de su centro. |
| **Jefatura (Centro Sur)** | `jefatura.sur` | Consulta de casos, investigaciones y planes de su centro. |

Las contraseñas se configuran fuera del repositorio. Para desarrollo, consulta `backend/.env.example`; en una instalación existente, solicita las credenciales al responsable del sistema.

---

## Reconocimiento de Evidencias Asistido por IA (OCR)

El módulo de digitalización documental integra:
- **API Externa Neuronal**: OCR.space (Motor 2 con TextOverlay estructurado en español). Requiere `Ocr__ApiKey` en `.env`.
- **Motores Locales Alternativos**: PaddleOCR (PP-OCRv5 en Python) y Tesseract en Node.js para resiliencia y soberanía de datos.
- **Editor Interactivo**: Permite revisar y corregir la transcripción de la IA y recortar hasta 12 elementos gráficos (firmas y sellos) para generar un documento Word `.docx` editable con OpenXML respetando la distribución espacial original.

---

## Verificación y Calidad de Software

Ejecución de la suite completa de pruebas automáticas:

```powershell
# 1. Pruebas de integración de backend (72 pruebas pasando)
dotnet test backend/Giatep.Tests

# 2. Compilación estricta de frontend y chequeo de tipos TypeScript
cd frontend
npm run build
cd ..

# 3. Verificación de salud de la API en producción
curl.exe -i https://giatep-api.onrender.com/health
```

---

## Declaración de Uso de Inteligencia Artificial

En cumplimiento con los requerimientos éticos y metodológicos de la asignatura:
- Se emplearon herramientas de Inteligencia Artificial (Codex, Antigravity) como apoyo para análisis arquitectónico, diseño de interfaces accesibles (WCAG 2.2 AA), optimización de algoritmos de árboles causales acíclicos (DAG) y generación de datos sintéticos de prueba.
- Toda la arquitectura, modelos relacionales, reglas de negocio, endpoints y componentes frontend fueron rigurosamente revisados, comprendidos, probados y ajustados por el equipo.
- El registro histórico detallado de interacciones y decisiones se encuentra documentado en [AI_LOG.md](file:///AI_LOG.md).
