# ---- Etapa 1: compilar el frontend (esta imagen NO se entrega) ----
FROM node:24-alpine AS build
WORKDIR /app

# Solo los manifiestos: la capa de `npm ci` se cachea mientras no cambien las dependencias.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ---- Etapa 2: nginx SIN privilegios (usuario no-root), sirviendo los estáticos y reenviando /api a la API ----
FROM nginxinc/nginx-unprivileged:stable-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
