# syntax=docker/dockerfile:1
# Multi-stage build para GIATEP Frontend (React 19 + TypeScript + Vite)

# Etapa 1: Compilacion y construccion de la aplicacion
FROM node:24-alpine AS build
WORKDIR /app

# Instalar dependencias con bloqueo estricto
COPY package*.json ./
RUN npm ci

# Copiar archivos fuente y generar bundle optimizado
COPY . .
RUN npm run build

# Etapa 2: Servidor web Nginx Alpine para entrega estatica y proxy SPA
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Copiar distribucion de Vite y configuracion de servidor
COPY --from=build /app/dist .
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
