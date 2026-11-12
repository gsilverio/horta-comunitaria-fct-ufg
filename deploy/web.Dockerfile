# Frontend React/Vite servido pelo nginx, que também encaminha /api e /health à API. Contexto: raiz do repositório.
FROM node:24.15.0-alpine AS build
WORKDIR /web
COPY hortacomunitaria-webapp/package.json hortacomunitaria-webapp/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY hortacomunitaria-webapp/ ./
# Mesma origem do frontend: o nginx encaminha /api/v1 para a API.
ENV VITE_API_BASE_URL=/api/v1
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.29-alpine AS web
COPY deploy/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /web/dist /usr/share/nginx/html
EXPOSE 8080
