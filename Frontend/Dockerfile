FROM node:20-alpine AS build
WORKDIR /app

# Build-контекст — корень репозитория (см. docker-compose.yml/Railway: тот же
# подход, что и в backend/Dockerfile — Railway CLI аплоадит только от git-root),
# поэтому COPY-пути с префиксом Frontend/.
COPY Frontend/package.json Frontend/package-lock.json ./
RUN npm ci

COPY Frontend/ .

# VITE_API_BASE_URL "запекается" в статические файлы при сборке (Vite —
# compile-time env), поэтому передаётся как build ARG, а не runtime ENV.
# Дефолт — адрес backend, опубликованный docker-compose на хосте (5000:8080),
# т.к. код выполняется в браузере на хосте, а не внутри docker-сети.
ARG VITE_API_BASE_URL=http://localhost:5000/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html

# nginx.conf.template в /etc/nginx/templates/ — штатный механизм официального
# образа nginx: entrypoint-скрипт сам гоняет envsubst и кладёт результат в
# /etc/nginx/conf.d/default.conf при старте контейнера. Нужно, т.к. Railway
# даёт порт через PORT (разный при каждом деплое), а не фиксированный 80 —
# захардкоженный "listen 80" ловил 502 (прокси стучится не в тот порт).
# ENV PORT=80 — дефолт для docker-compose/локального docker run, где PORT
# не задаётся; Railway всегда прокидывает свой PORT и переопределяет его.
COPY Frontend/nginx.conf.template /etc/nginx/templates/default.conf.template
ENV PORT=80

EXPOSE 80
