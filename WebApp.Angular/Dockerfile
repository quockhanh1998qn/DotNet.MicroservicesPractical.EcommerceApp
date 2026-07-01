# --- build stage ---
FROM node:20-alpine AS build
WORKDIR /app

COPY WebApp.Angular/package.json WebApp.Angular/package-lock.json* ./
RUN npm install --no-audit --no-fund

COPY WebApp.Angular/ .
RUN npm run build -- --configuration production

# --- runtime (nginx) ---
FROM nginx:1.27-alpine AS final
COPY WebApp.Angular/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/tedu-webapp/browser /usr/share/nginx/html
EXPOSE 80
