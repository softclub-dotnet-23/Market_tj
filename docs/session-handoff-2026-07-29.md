# Market.tj — Handoff-отчёт (2026-07-29)

## Что это за проект

Market.tj — маркетплейс "фермер → покупатель" для Таджикистана. Backend: .NET 10 / ASP.NET Core Web API, Clean Architecture (`MarketTJ.Domain` → `MarketTJ.Application` → `MarketTJ.Infrastructure` → `MarketTJ.WebApi`), EF Core + Npgsql (PostgreSQL), Redis (кэш каталога), xUnit + Moq. Frontend: React + Vite + TypeScript, i18next (RU/TJ/EN). Роли: Admin, Farmer, Customer, Courier. Репозиторий: `softclub-dotnet-23/Demo-Project` (GitHub сообщает "moved to Market_tj.git", но push на старый URL по-прежнему проходит), рабочая ветка — `Backend`.

Деплой — Railway (проект `MarketTJ`, workspace "Nekruz's Projects"):
- Backend: https://backend-production-ca720.up.railway.app
- Frontend: https://frontend-production-431d.up.railway.app

## Текущий статус деплоя

Проверено только что (2026-07-29):
- **Backend** — ● Online, `GET /health` → `200 Healthy`, чистый старт в логах (`Application started`, `Hosting environment: Production`), логин и все проверенные эндпоинты работают.
- **Frontend** — ● Online, `GET /` → `200`.
- **Postgres** — ● Online (volume `postgres-volume-oJst`, 83 MB / 500 MB).
- **Redis** — ● Online (`redis-volume`).

Backend env vars (только имена, не значения): `ConnectionStrings__RedisCache`, `DATABASE_URL`, `FRONTEND_URL`, `Jwt__Secret`, плюс стандартные `RAILWAY_*`. **`ADMIN_PASSWORD` не установлена** — значит, аккаунт `admin@market.tj` на проде сейчас использует захардкоженный дефолтный пароль из `Seeder.cs` (`Admin@12345!`), а не случайный/секретный — см. "Важно знать" ниже.

## Backend — что готово

**Domain/Entities (30 сущностей):** User, RefreshToken, FarmerProfile, CustomerProfile, CourierProfile, FarmerStaffMember, FarmerDocument, Category, Product, ProductListing, ProductImage, CartItem, Order, OrderItem, Payment, RefundRequest, Delivery, DeliverySlot, DeliveryZone, Review, ReportedListing, Favorite, Notification, Conversation, ChatMessage, SupportTicket, SupportMessage, Commission, AppSetting, AuditLog, DailySalesSnapshot.

**Application/Dto:** 33 группы DTO (~136 файлов) — по каждой сущности `Create.../Update.../Delete.../Get...Dto`, единообразно.

**Application/Services (32 файла) + Validators (31 файлов):** все интерфейсы (`I{Entity}Service`, 36 штук) имеют реализацию — `ICacheService`/`ITokenService`/`ICurrentUserService`/`IFileStorageService` реализуются в Infrastructure/WebApi (не в Application/Services), остальные 32 — все реализованы, **нет ни одной заглушки/NotImplementedException**. Grep по `TODO|FIXME|NotImplementedException` в `backend/**/*.cs` — 0 совпадений, кодовая база чистая от незакрытых заметок.

**Api/Controllers — 39 контроллеров** (34 в корне + 5 в `Controllers/Admin/`):

| Роут | Роли | Заметки |
|---|---|---|
| `api/auth` | `[AllowAnonymous]` | login/register/refresh/logout |
| `api/me` | `[Authorize]` | текущий пользователь |
| `api/users` | Admin | `GetAll()` без пагинации — дублирует `api/admin/users` (см. аудит 3.2) |
| `api/categories`, `api/products`, `api/farmer-profiles`, `api/product-listings`, `api/product-images`, `api/delivery-zones`, `api/reviews` | пишущие роли Admin/Farmer, **чтение публично** (`[AllowAnonymous]` на Get) | каталог/витрина — намеренно открыт |
| `api/farmer-documents`, `api/farmer-staff-members` | Farmer,Admin | IDOR-guard применён (2026-07 аудит) |
| `api/customer-profiles` | Customer,Admin | IDOR-guard применён |
| `api/courier-profiles` | Courier,Admin | IDOR-guard применён |
| `api/cart-items`, `api/favorites`, `api/orders`, `api/order-items`, `api/notifications`, `api/conversations`, `api/chat-messages`, `api/reported-listings`, `api/refund-requests`, `api/support-tickets`, `api/support-messages`, `api/payments`, `api/deliveries` | `[Authorize]` (без ролей — доступ разруливается на уровне сервиса через `ICurrentUserService`) | IDOR-guard применён во всех |
| `api/delivery-slots` | смешанные атрибуты на методах (`[Authorize]` на Get, `[Authorize(Roles="Admin")]` на CUD) | |
| `api/analytics` | `[Authorize]`, конкретные методы Admin/Farmer | дашборды |
| `api/ai-assistant` | `[Authorize]` | backend есть, **фронтенд не подключён** (см. ниже) |
| `api/app-settings`, `api/audit-logs`, `api/commissions`, `api/daily-sales-snapshots` | Admin | |
| `api/admin/users`, `api/admin/audit-logs`, `api/admin/reported-listings`, `api/admin/support-tickets`, `api/admin` (orders+refunds) | Admin | пагинированные admin-списки + модерационные действия, пишут в `AuditLog` |

**Middleware:** `ExceptionHandlingMiddleware` (первым — скрывает stack trace в Production) → `RequestLoggingMiddleware` → (Swagger только в Development) → `HttpsRedirection` → `StaticFiles` → `Cors` → `Authentication` → `Authorization` → `MapControllers` + `MapHealthChecks("/health")` (анонимный).

**Auth:** полноценный JWT (access + refresh, ротация refresh-токена, `RefreshToken` в БД), `[Authorize]`/`[Authorize(Roles=...)]` расставлены на всех 39 контроллерах (кроме `AuthController`). Роли: `Admin=1, Farmer=2, Customer=3, Courier=4`.

**IDOR-защита (закрыта в этой/прошлой сессии, аудит 2026-07-28 находка 2.2):** общий helper `CurrentUserAuthorizationExtensions.CanAccess(ownerId)`/`IsAdmin()`, `ErrorType.Forbidden → 403`. Применена во всех 21 ранее уязвимых сервисах + добавлены негативные тесты. Публичные витрины (Review/FarmerProfile/ProductListing/ProductImage `GetAll/GetById`) намеренно не тронуты.

## Backend — что НЕ готово / известные пробелы

Из аудита `docs/backend-code-audit-2026-07-28.md` (см. раздел ниже) **не закрыто**:
- **2.3 — EF Core query filter cascade**: 21 связь (User/ProductListing/Order ↔ их дочерние сущности) без согласованного `HasQueryFilter` — потенциальный `NullReferenceException` при soft-delete родителя.
- **2.4 — нет rate limiting на `/api/auth/login`** — открыт для brute-force/credential-stuffing (email админа предсказуем: `admin@market.tj`).
- 🟡 3.2 — дублирующийся `GET /api/users` vs `GET /api/admin/users`.
- 🟡 3.3 — большинство `GetAll()` не пагинированы (кроме `ProductListing` и `Admin/*`).
- 🟡 3.4/3.6/4.9 — отсутствующие индексы на `Status`/`IsActive`/`Role`, N+1-паттерны (`GetAllAsync().Where(...)` в памяти вместо точечных запросов) в `OrderService`, `CartItemService`, `UserService`.
- Нет тестового файла `AiAssistantServiceTests.cs` (единственный сервис без покрытия — внешний HTTP-вызов к Anthropic API не замокан).
- `ProductListing` не имеет статуса "на модерации" — точечная приёмка нового товара не реализована, модерация есть только через `ReportedListing` (жалобы постфактум).

## Frontend — что готово / что не готово

**Готово:**
- Полный i18n: RU/TJ/EN, `Frontend/src/locales/{ru,tj,en}/*.json` (common/layout/ui/product/sections/pages/admin/farmer/customer/data), `react-i18next`, `LanguageSwitcher.tsx`, реально используется в 83 файлах.
- Страницы: публичные (Home/Catalog/ProductDetails/About/Contact/FarmerPublicProfile), Auth (Login/Register), Customer-кабинет (Dashboard/Orders/Profile/Messages/Notifications), Farmer-кабинет (Dashboard/Products/Orders/Documents/Staff/Reviews/Messages/Notifications/Profile), Admin-панель (Dashboard/Users/Farmers/Products/Orders/Reviews/Commissions/Couriers/DeliveryZones/Statistics/Settings/Notifications), чат (`ChatModal`/`ConversationsList`), уведомления (`NotificationCenter`).
- Контексты: Auth/Cart/Favorites/Theme/Language.

**Не готово / пробелы:**
- **Нет Courier-кабинета вообще** — ни одной страницы `Courier*.tsx`, хотя роль `Courier`, `CourierProfile`, `CourierProfileController`/`DeliveryController` полностью реализованы на backend.
- **Нет AI-виджета на фронтенде** — `AiAssistantController`/`AiAssistantService` существуют на backend (вызывают Anthropic API), но ни одного компонента-потребителя (`Assistant`/`Chatbot` и т.п.) во фронтенде не найдено.

## Безопасность — статус

Полный отчёт: [docs/backend-code-audit-2026-07-28.md](../docs/backend-code-audit-2026-07-28.md) (5 🔴 Critical, 11 🟡 Warning, 9 🔵 Suggestion).

| # | Находка | Статус |
|---|---|---|
| 2.1 | Admin-эндпоинт хранил пароль без хэширования | ✅ Исправлено (коммит `7fbc9db`) |
| 2.2 | Системный IDOR на 21 контроллере | ✅ Исправлено (коммит `7fbc9db`) |
| 2.3 | EF Core query filter cascade (21 связь) | ❌ Не исправлено |
| 2.4 | Нет rate limiting на `/api/auth/login` | ❌ Не исправлено |
| 2.5 | `.dockerignore` отсутствовал, реальный пароль Postgres в appsettings.json | ⚠️ Частично — см. ниже, **появился новый риск** |

**Новый риск, обнаруженный и НЕ устранённый в этой сессии:** при редеплое 2026-07-29 обнаружилось, что базовый `appsettings.json` был в `.gitignore` целиком (не только `Development`), поэтому **никогда не попадал в Docker-образ на Railway** → `Jwt:ExpiryMinutes` отсутствовал в контейнере → `500` на каждом логине. Чтобы это исправить, `.gitignore`/`backend/.dockerignore` были сужены (теперь исключают только `appsettings.Development.json`), и **базовый `appsettings.json` был закоммичен и запушен в GitHub** (коммит `7fbc9db`). Этот файл всё ещё содержит строку (аудит-находка 2.5 её же и описывала):
```
"DefaultConnection": "Host=localhost;Port=5432;Database=markettj;Username=postgres;Password=07806634"
```
— судя по всему, реальный локальный пароль разработчика от Postgres, **теперь опубликованный в git-истории репозитория**. Он не используется в Production (Railway подставляет `DATABASE_URL` поверх), но если это боевой/переиспользуемый где-то пароль — стоит его сменить и/или заменить значение в `appsettings.json` на плейсхолдер (`postgres`/`changeme`) отдельным коммитом.

## Тесты — статус

`dotnet build -c Debug` — **0 ошибок, 0 предупреждений**.
`dotnet build -c Release` — **0 ошибок, 0 предупреждений**.
`dotnet test` — **814/814 пройдено**, 0 упавших, 0 пропущенных (~1 сек).

(Для сравнения: на момент аудита 2026-07-28 было 806/806 — 8 новых тестов добавлены вместе с IDOR-фиксом.)

## Что было сделано в последних сессиях

⚠️ **`PROGRESS.md` (и корневой, и `docs/PROGRESS.md`) устарели** — последняя запись датирована 2026-07-22 ("admin-действия, pagination, Swagger", 743/743 тестов). Они **не отражают** ни i18n (RU/TJ/EN), ни адаптацию под Railway, ни сам деплой, ни аудит безопасности, ни фикс IDOR/паролей — все эти этапы делались в последующих сессиях, но PROGRESS.md не обновлялся. Реальная история — только в `git log`:

```
7fbc9db fix critical security issues: password hashing and IDOR (audit 2026-07-28)
7f945a0 add railway config
f1a82e1 add full backend code audit report
64629fe adapt project for Railway
8490d4f Merge branch 'main' ... into Backend
315ee80 Merge pull request #13 from Frontend
0a0cab3 Fix bugs
8f86f95 add full i18n support (ru/tj/en)
1ce9301 Seed test orders/payments/reviews so admin dashboard isn't empty
...
9104456 add admin management actions, pagination, swagger   ← последняя запись PROGRESS.md отсюда
286477c add JWT auth and enforce authorization
```

Кратко по сессиям (не из PROGRESS.md, а по факту работы в чате):
1. Полный i18n (RU/TJ/EN) на фронтенде.
2. Адаптация под Railway (dynamic PORT, DATABASE_URL-парсинг, CORS по FRONTEND_URL, build ARG для фронта) + реальный деплой через Railway CLI (backend, frontend, Postgres, Redis, все URL связаны). По пути найдены и исправлены: build-context mismatch (оба Dockerfile переписаны под git-root контекст), баг `WebRootPath` (порядок `Directory.CreateDirectory` относительно `CreateBuilder`), отсутствие Redis-сервиса (500 на `/api/categories`), JWT-секрет не подхватывался в Production (появился отдельно от локального dev-фикса), nginx хардкодил порт 80 (502 на фронте — исправлено через envsubst-темплейт).
3. Полный аудит backend (`docs/backend-code-audit-2026-07-28.md`) — 5 критических находок.
4. Фикс двух самых критичных находок аудита (2.1 хэширование паролей, 2.2 IDOR на 21 контроллере) + 8 новых тестов + редеплой на Railway. В процессе редеплоя обнаружен и исправлен независимый баг (appsettings.json исключён из git/Docker-образа целиком — см. "Безопасность" выше).

## Что делать дальше (приоритетный список)

1. **Срочно:** решить, что делать с реальным Postgres-паролем `07806634`, теперь опубликованным в `appsettings.json` в git-истории (сменить пароль и/или заменить на плейсхолдер отдельным коммитом).
2. Установить `ADMIN_PASSWORD` как секретную переменную окружения на Railway backend-сервисе (сейчас там дефолт из кода `Admin@12345!`).
3. Закрыть аудит-находку 2.4 — rate limiting на `/api/auth/login` (`Microsoft.AspNetCore.RateLimiting`, `FixedWindowLimiter`/`SlidingWindowLimiter` по IP+email).
4. Закрыть аудит-находку 2.3 — `HasQueryFilter` на 21 связи (User/ProductListing/Order ↔ дочерние сущности), требует решения по каждой сущности отдельно.
5. Frontend: решить, нужен ли Courier-кабинет (backend полностью готов, фронтенда нет вообще) и AI-виджет (аналогично — backend есть, потребителя нет).
6. Обновить `PROGRESS.md`/`docs/PROGRESS.md` — они отстают минимум на 4 крупные сессии работы.
7. Разобрать 🟡-находки аудита (пагинация всех GET-списков, индексы на `Status`/`IsActive`/`Role`, устранение `GetAllAsync()`-в-памяти паттернов).

## Важно знать перед продолжением

- **403 vs 404 — намеренное решение (2026-07-29):** для "чужого ресурса" используется `403 Forbidden` (`ErrorType.Forbidden`), а не `404` — 401 зарезервирован строго за "не аутентифицирован/битый токен". Задокументировано как проектная конвенция в самом коде (`ErrorType.cs`).
- **`.gitignore`/`backend/.dockerignore` сузились в этой сессии** — раньше исключали весь `appsettings*.json` (включая базовый файл без секретов), из-за чего Production-контейнер на Railway не получал `Jwt:ExpiryMinutes` и падал на каждом логине с `500`. Теперь исключается только `appsettings.Development.json` (там реальный dev-JWT-секрет). Если в новой сессии снова появится `500` на логине после Railway-редеплоя — первым делом проверить, не попал ли `appsettings.json` снова в исключения.
- **Git в тул-окружении этой сессии одно время был полностью недоступен** (`git` не резолвился ни через `Get-Command`, ни по прямому пути, несмотря на попытки `winget install`), затем — без видимой причины — снова заработал (`D:\Git\cmd\git.exe`). Если в новой сессии git снова "исчезнет" — это не повод считать репозиторий сломанным, вероятно временное ограничение окружения; стоит попробовать через некоторое время или попросить пользователя выполнить `git`-команды из его собственного терминала.
- **`railway up` деплоит с локального диска, а не из git** — рассинхрон "закоммичено ли" не блокирует редеплой, но означает, что о состоянии прод-контейнера нельзя судить по `git log`/`git status`.
- **`.claude/skills/` в этом репозитории не существует вообще** (проверено — только `.claude/launch.json` для превью-браузера). Skill `market-tj-code-review`, упомянутый пользователем в одной из прошлых сессий, либо существует только в его глобальном Claude-конфиге (не в этом репозитории), либо это было ошибочное предположение — не полагаться на его наличие здесь.
- **Swagger доступен только в Development** (`if (app.Environment.IsDevelopment())` в `Program.cs`) — на Railway (`ASPNETCORE_ENVIRONMENT=Production`) Swagger UI недоступен; для ручной проверки прод-эндпоинтов нужны прямые HTTP-запросы (curl/Invoke-RestMethod), не Swagger.
- **Тестовые аккаунты (сидированы, пароли — открытым текстом в `Seeder.cs`, не секрет):** `admin@market.tj`, `farmer@market.tj`, `customer@market.tj`, `courier@market.tj` — пароли вида `{Role}@12345!` (кроме admin — см. пункт про `ADMIN_PASSWORD` выше).
