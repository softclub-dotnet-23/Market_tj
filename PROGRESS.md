# Прогресс Market.tj

## 2026-07-21 — Docker-поддержка для локального запуска + система учёта прогресса
- Сделано:
  - `backend/MarketTJ.WebApi/Dockerfile` — multi-stage (`mcr.microsoft.com/dotnet/sdk:10.0` → `mcr.microsoft.com/dotnet/aspnet:10.0`), `ENV ASPNETCORE_URLS=http://+:8080`, `EXPOSE 8080`.
  - `backend/.dockerignore` — исключает `bin/`, `obj/` и, что важно, `appsettings*.json` (локальные секреты не должны попадать в образ — конфигурация в контейнере идёт через переменные окружения).
  - `Frontend/Dockerfile` — multi-stage (`node:20-alpine` → `nginx:alpine`), `ARG VITE_API_BASE_URL` (бейкается в статику при сборке), `EXPOSE 80`.
  - `Frontend/nginx.conf` — SPA fallback (`try_files $uri $uri/ /index.html`) для react-router.
  - `Frontend/.dockerignore` — исключает `node_modules/`, `dist/`.
  - `docker-compose.yml` в корне — сервисы `db` (postgres:16-alpine, healthcheck, volume `db-data`), `redis` (redis:7-alpine — реально используется в проекте, `ICacheService`/`RedisCacheService`), `backend` (порт 5000:8080, `ConnectionStrings__DefaultConnection`/`ConnectionStrings__RedisCache` через env, `depends_on: db (service_healthy)`), `frontend` (порт 3000:80, build-arg `VITE_API_BASE_URL=http://localhost:5000/api`), общая сеть `markettj`.
  - `.env.example` — шаблон переменных (`POSTGRES_USER/PASSWORD/DB`, `ANTHROPIC_API_KEY`); `.env` добавлен в `.gitignore`.
  - Создан корневой `PROGRESS.md` (этот файл) и настроена привычка синхронизировать его с `TZ_MarketTJ_ClaudeCode.md` после каждой завершённой задачи.
  - `TZ_MarketTJ_ClaudeCode.md`: раздел 12 дополнен подразделом «Развёртывание (Docker)»; раздел 38 дополнен пунктом про появление корневого `PROGRESS.md`.
- Проблемы/блокеры:
  - Docker Desktop в этом окружении не может запустить демон (`Error response from daemon: Docker Desktop is unable to start`) — похоже, среда не поддерживает нужную виртуализацию. `docker compose build`/`up` физически не выполнены и не проверены в контейнерах.
  - Вместо этого проверено то, что было можно: `docker compose config` — конфигурация валидна и корректно резолвится; `dotnet publish` (Release, те же аргументы, что в Dockerfile) — проходит успешно; `npm run build` (с тем же `VITE_API_BASE_URL`, что передаётся как build-arg) — проходит успешно, `dist/` собирается.
- Что осталось на следующую сессию:
  - Прогнать `docker compose build && docker compose up -d` на машине/окружении, где Docker Desktop реально работает; проверить `curl` к backend-эндпоинту и открытие frontend в браузере.
  - Тот же fix bin/obj (`.gitignore` + untrack), что уже сделан на ветке `Frontend`, применить на ветке `Backend` (предлагалось ранее, ещё не подтверждено пользователем).
