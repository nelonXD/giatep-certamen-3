# syntax=docker/dockerfile:1
# Multi-stage build para GIATEP Backend (.NET 10)

# Etapa 1: Compilacion y publicacion
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias
COPY ["backend/Giatep.Api/Giatep.Api.csproj", "backend/Giatep.Api/"]
COPY ["shared/denuncias.json", "shared/denuncias.json"]
RUN dotnet restore "backend/Giatep.Api/Giatep.Api.csproj"

# Copiar codigo fuente y compilar en Release
COPY backend/Giatep.Api/ backend/Giatep.Api/
WORKDIR "/src/backend/Giatep.Api"
RUN dotnet publish "Giatep.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Imagen final de ejecucion optimizada (Runtime liviano y seguro)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Instalar curl para verificaciones de salud activas del contenedor (Health Checks)
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Crear directorios para persistencia de evidencias locales con permisos plenos para usuario app
RUN mkdir -p /app/storage/evidencias && chown -R app:app /app/storage && chmod -R 775 /app/storage

# Usuario no privilegiado para cumplir buenas practicas de seguridad (OWASP)
USER app

ENTRYPOINT ["dotnet", "Giatep.Api.dll"]
