# syntax=docker/dockerfile:1
# Multi-stage build para GIATEP Backend (.NET 10)

# Etapa 1: Compilación y publicación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias
COPY ["backend/Giatep.Api/Giatep.Api.csproj", "backend/Giatep.Api/"]
COPY ["shared/denuncias.json", "shared/denuncias.json"]
RUN dotnet restore "backend/Giatep.Api/Giatep.Api.csproj"

# Copiar código fuente y compilar
COPY backend/Giatep.Api/ backend/Giatep.Api/
WORKDIR "/src/backend/Giatep.Api"
RUN dotnet publish "Giatep.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Imagen final de ejecución optimizada
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

# Usuario no privilegiado para cumplir buenas prácticas de seguridad (OWASP)
USER app

ENTRYPOINT ["dotnet", "Giatep.Api.dll"]
