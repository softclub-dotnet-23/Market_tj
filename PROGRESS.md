# Прогресс Market.tj

## 2026-07-22 — Второй merge-конфликт origin/main → Backend (remember-me, роль Farmer, admin/farmer-дашборды)
- Сделано:
  - После предыдущего merge-коммита пришёл ещё один `git pull origin main` (пользователь запустил его сам), снова зацепивший те же 3 файла: `AuthContext.tsx`, `lib/api.ts`, `pages/Login.tsx` — плюс безконфликтно добавился большой пласт новых страниц (`AdminStatistics`, `AdminOrders`, `AdminProducts`, `AdminFarmers`, `AdminUsers`, `AdminCommissions`, `AdminReviews`, `AdminSettings`, `FarmerDashboard`, `FarmerProducts`, `FarmerLayout`).
  - Конфликты разрешены в пользу `main`: "запомнить меня" (localStorage/sessionStorage), редирект по трём ролям (Admin/Farmer/остальные) через `window.location.href` вместо SPA-навигации (нужно для надёжного предложения браузера сохранить пароль).
  - `lib/api.ts` снова оставлен в виде универсального клиента (`api.get/post/put/delete`) — но новые `data/adminEntities.ts` и `data/farmer.ts` (из `main`, без конфликта) вызывают `apiGet`/`apiPost` напрямую, поэтому добавлены тонкие алиасы `export const apiGet = api.get; export const apiPost = api.post;` вместо переписывания чужого нового кода.
  - `AuthUser.id` переименован в `AuthUser.userId` — под это поле уже написан `data/farmer.ts` (`user?.userId`), это единственный реальный потребитель.
  - `backend/MarketTJ.WebApi/Dockerfile` оказался staged как переименование в `backend/Dockerfile` (содержимое идентично — просто перемещённый файл, не конфликт слияния) — поправлена ссылка `dockerfile:` в `docker-compose.yml`.
  - Починены ещё 2 однотипные TS-ошибки `recharts`-formatter'а в `AdminStatistics.tsx`/`FarmerDashboard.tsx` (тот же паттерн, что раньше в `AdminDashboard.tsx`).
  - Проверено: `dotnet build` — 0 ошибок; `dotnet test` — 743/743; `npm run build` — успешно.
- Проблемы/блокеры: нет.
- Что осталось на следующую сессию:
  - Не запушено — коммиты локально на ветке `Backend`.
  - Токен всё ещё не подставляется в `Authorization`-заголовок для защищённых запросов (актуально теперь для всех новых Admin*/Farmer*-страниц, использующих `apiGet`/`apiPost`).

## 2026-07-21 — Разрешён конфликт слияния origin/main → Backend (реальный JWT вместо заглушки)
- Сделано:
  - Разблокирован `git pull origin main`: на ветке `Backend` `bin/`/`obj/` не были в `.gitignore` и мешали merge — добавлены паттерны `backend/*/bin/`, `backend/*/obj/` в `.gitignore`, файлы сняты с отслеживания (`git rm -r --cached`), как уже было сделано ранее на ветке `Frontend`.
  - После этого `git pull` дал реальные конфликты (не тривиальные): на `Backend` авторизация была заглушкой без токена (комментарии в коде прямо это фиксировали: "TODO: заменить на JWT, когда появится"), `main` принёс полноценную JWT-реализацию. Разрешено в пользу `main` для: `AuthService.cs`, `LoginResponseDto.cs`, `AuthController.cs`, `AuthContext.tsx`, `Login.tsx`, роутинга в `App.tsx` (`ProtectedRoute`/`AdminLayout` вместо старого `RequireAdmin`), DI-регистрации `ITokenService` вместо `IPasswordHasher`.
  - Там, где обе стороны добавили разное, но нужное одновременно, — объединено, а не выбрана одна сторона: `Program.cs` сохранил CORS-политику (была только в `Backend`) вместе с JWT-мидлварой (была только в `main`), в правильном порядке (`UseCors` → `UseAuthentication` → `UseAuthorization`); `Frontend/src/lib/api.ts` оставлен в варианте `Backend` (универсальный клиент `get/post/put/delete`, `ApiError`, `VITE_API_BASE_URL` для Docker) — вариант `main` (`apiPost`, только для логина, захардкоженный `/api`) сломал бы уже собранный Docker-билд фронтенда.
  - Удалены файлы, ставшие мёртвым кодом после разрешения конфликта (нулевые оставшиеся ссылки, проверено grep): `RequireAdmin.tsx`, `pages/admin/AdminDashboard.tsx`, `IPasswordHasher.cs`, `BCryptPasswordHasher.cs`.
  - Добавлена секция `Jwt` (`Issuer`/`Audience`/`Secret`/`ExpiryMinutes`) в локальный `appsettings.json` (не в git) и `JWT_SECRET` в `docker-compose.yml`/`.env.example`/`.env` — без этого смёрженный код не запускался бы (конфиг был только локально у автора `main`).
  - Попутно исправлены две TS-ошибки сборки, всплывшие после `npm install` (не связаны с Auth): типы `formatter` у `Tooltip` из `recharts` в `AdminDashboard.tsx`, конфликт типов `startViewTransition` в `ThemeContext.tsx` (новая версия `lib.dom.d.ts`).
  - Проверено: `dotnet build` — 0 ошибок; `dotnet test` — 743/743 пройдено; `npm run build` (`tsc -b && vite build`) — успешно.
- Проблемы/блокеры: нет.
- Что осталось на следующую сессию:
  - Токен из `AuthContext` пока нигде не подставляется в заголовок `Authorization` для последующих защищённых запросов (кроме самого логина) — `api.ts` это не делает автоматически, понадобится для любых будущих `[Authorize]`-эндпоинтов на фронте.
  - Не запушено — коммиты остались локально на ветке `Backend`.

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
