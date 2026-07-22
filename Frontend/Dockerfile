FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# VITE_API_BASE_URL "запекается" в статические файлы при сборке (Vite —
# compile-time env), поэтому передаётся как build ARG, а не runtime ENV.
# Дефолт — адрес backend, опубликованный docker-compose на хосте (5000:8080),
# т.к. код выполняется в браузере на хосте, а не внутри docker-сети.
ARG VITE_API_BASE_URL=http://localhost:5000/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
