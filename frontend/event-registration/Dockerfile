FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm install

COPY . .

# Vite inyecta las variables VITE_* en tiempo de build.
ARG VITE_EVENT_SERVICE_URL=http://localhost:5101
ENV VITE_EVENT_SERVICE_URL=$VITE_EVENT_SERVICE_URL
ARG VITE_OIDC_AUTHORITY=http://localhost:8180/realms/plataforma-eventos
ARG VITE_OIDC_CLIENT_ID=event-registration-spa
ENV VITE_OIDC_AUTHORITY=$VITE_OIDC_AUTHORITY
ENV VITE_OIDC_CLIENT_ID=$VITE_OIDC_CLIENT_ID
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
