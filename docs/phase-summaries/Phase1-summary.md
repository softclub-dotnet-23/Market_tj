# Phase 1 — Backend Domain/CRUD, тестирование, Analytics, Auth (частично), связка с Frontend Summary

**Status:** 🚧 In Progress
**Completed:** 2026-07-20
**Branch:** Backend
**Tag:** —

> Примечание: эта фаза не совпадает один-в-один с последовательностью «Этап 1 → Этап 9» из раздела 23 ТЗ.
> Сделано существенно больше, чем «Этап 1. Основа проекта» (весь CRUD-слой по всем 30 сущностям, 743 unit-теста,
> Analytics, первый срез Auth и Frontend), но при этом не закрыты формальные критерии более поздних этапов
> (JWT, role-authorization, полноценные Farmer/Customer/Courier кабинеты). Считать это «горизонтальным»
> проходом по всей бизнес-модели вместо строго последовательного — решение зафиксировано здесь как отклонение
> от процесса раздела 37, чтобы не потерять контекст при чтении в будущей сессии.

---

## What Was Built

### Backend

| Item | Description |
|------|-------------|
| Domain — 30 entities + enums | Все сущности раздела 8 ТЗ (`User`, `FarmerProfile`, `CustomerProfile`, `CourierProfile`, `Category`, `Product`, `ProductListing`, `ProductImage`, `CartItem`, `Order`, `OrderItem`, `Delivery`, `Review`, `Notification`, `Conversation`, `ChatMessage`, `AppSetting`, `FarmerDocument`, `AuditLog`, `ReportedListing`, `RefundRequest`, `DeliveryZone`, `Commission`, `Payment`, `Favorite`, `FarmerStaffMember`, `SupportTicket`, `SupportMessage`, `DeliverySlot`, `DailySalesSnapshot`) + все enum'ы (`UserRole`, `OrderStatus`, `PaymentStatus`, `ListingStatus`, `FarmerVerificationStatus` и т.д.) |
| Persistence (EF Core + Npgsql) | `AppDbContext` с 30 `DbSet<T>`, 30 `IEntityTypeConfiguration<T>` (FK, soft-delete `HasQueryFilter` на `User`/`Order`/`ProductListing`, unique-индексы, precision для decimal), 2 миграции применены к PostgreSQL |
| Repositories | 30 `I{Entity}Repository`/`{Entity}Repository` — единый generic CRUD (`GetAllAsync/GetByIdAsync/AddAsync/UpdateAsync/DeleteAsync`), без выдуманных методов сверх интерфейса. Отдельно — `IAnalyticsRepository`/`AnalyticsRepository` с агрегацией через LINQ (`GroupBy/Sum/Count`) прямо на `AppDbContext` |
| DTO | `Create/Update/Get{Entity}Dto` на все 30 сущностей + `AuthDto` (`LoginRequestDto`/`LoginResponseDto`) + `AnalyticsDto` (`AdminDashboardDto`, `FarmerDashboardDto`, `TopSellingProductDto`, `OrderStatusCountDto`, `MonthlyRevenueDto`) |
| Validators | 30 статических `{Entity}Validator` — обязательные поля, длины строк, диапазоны чисел, `Enum.IsDefined`, email/формат-regex, FK-проверки, уникальность, бизнес-правила по разделам ТЗ (например: один Review/Delivery/Conversation/DeliverySlot на Order, рейтинг 1–5, RejectionReason обязателен при Rejected и т.п.) |
| Application Services | 30 `{Entity}Service` (CRUD, try/catch + `logger.LogError`, `Result<T>`, soft-delete для `User`/`Order`/`ProductListing`) + `AnalyticsService` (агрегированная статистика) + `AuthService` (проверка логина через BCrypt) + `AiAssistantService` (интеграция с Anthropic Messages API — осознанное отклонение от MVP-scope раздела 3, подтверждено пользователем) |
| WebApi Controllers | 30 CRUD-контроллеров (`GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}`) + `AuthController` (`POST /api/auth/login`) + `AnalyticsController` (`GET /api/analytics/admin/dashboard`, `GET /api/analytics/farmer/dashboard`) + `AiAssistantController`. Общий `ApiControllerBase.HandleResult<T>` маппит `Result<T>` → `IActionResult` (200/400/401/404/409/500) без дублирования в каждом контроллере |
| Middleware | `ExceptionHandlingMiddleware` (глобальный перехват, единый JSON-ответ, без stack trace в Production) и `RequestLoggingMiddleware` (метод/путь/статус/мс, уровень Information) — оба зарегистрированы в `Program.cs` первыми в pipeline |
| Database Seeder | `Seeder.cs` — 4 пользователя (по одному на роль), профили, 6 категорий, товары, 16 объявлений от тестового фермера; идемпотентен (`AnyAsync`-проверки) |
| CORS | Политика `FrontendCorsPolicy` из `Cors:AllowedOrigins` (appsettings) — разрешает `http://localhost:5173`/`4173` (Vite dev/preview), без credentials (Auth ещё не выдаёт cookie/token) |
| Auth (частично) | `POST /api/auth/login` — сверяет email+пароль через `IPasswordHasher`/`BCryptPasswordHasher` (BCrypt.Net-Next) против `Users`, возвращает `LoginResponseDto` **без токена** — полноценного JWT/Identity ещё нет (см. Known Issues) |
| Unit-тесты | 743 xUnit-теста (Moq для репозиториев/логгера) — по одному test-классу на каждый из 30 `{Entity}Service`, покрывают Get/Create/Update/Delete, каждое правило валидатора отдельным тестом, FK/уникальность/бизнес-правила, обработку исключений |

### Frontend

| Item | Description |
|------|-------------|
| Базовый каркас (не создан в этой фазе) | Vite + React 19 + TypeScript + Tailwind, i18n (ru/tj), каталог/маркетинговые страницы (`Home`, `Catalog`, `ProductDetails`, `About`, `Contact`) на статических моках (`src/data/*.ts`) — уже существовал на момент подключения к backend, содержимое не менялось |
| API-клиент | `src/lib/api.ts` — fetch-обёртка под конверт ответа backend (`{isSuccess, message, data}` / `{isSuccess:false, message, errors}`), `VITE_API_BASE_URL` из `.env` |
| AuthContext | `src/context/AuthContext.tsx` — `login()`/`logout()`, пользователь персистится в `localStorage` (временно, до появления JWT) |
| Login | `src/pages/Login.tsx` — реальный вызов `POST /api/auth/login` вместо прежнего фейкового `setTimeout`-тоста; при роли `Admin` редирект на `/admin`, для остальных ролей — заглушка «в разработке» |
| Route guard | `src/components/auth/RequireAdmin.tsx` — закрывает `/admin` для неавторизованных/не-Admin, редирект на `/login` |
| Admin Dashboard | `src/pages/admin/AdminDashboard.tsx` — реальные данные из `GET /api/analytics/admin/dashboard` (пользователи по ролям, заказы, выручка, топ продаж, статусы заказов, график по месяцам), logout |

---

## Key Files

**Backend**
- `backend/MarketTJ.Domain/Entities/*.cs`, `backend/MarketTJ.Domain/Enums/*.cs`
- `backend/MarketTJ.Infrastructure/Persistence/AppDbContext.cs`, `.../Configurations/*.cs`, `.../Repositories/*.cs`, `.../Seed/Seeder.cs`
- `backend/MarketTJ.Infrastructure/Migrations/20260716142427_InitialCreate.cs`, `.../20260717051859_Migrations.cs`
- `backend/MarketTJ.Infrastructure/Security/BCryptPasswordHasher.cs`
- `backend/MarketTJ.Application/Dto/**/*.cs`, `.../Validators/*.cs`, `.../Services/*.cs`, `.../Interfaces/**/*.cs`, `.../DependencyInjection.cs`
- `backend/MarketTJ.WebApi/Controllers/*.cs`, `.../Middleware/*.cs`, `.../Program.cs`, `.../appsettings.json`
- `backend/MarketTJ.Application.Tests/Services/*ServiceTests.cs`

**Frontend**
- `Frontend/.env`
- `Frontend/src/lib/api.ts`
- `Frontend/src/context/AuthContext.tsx`
- `Frontend/src/components/auth/RequireAdmin.tsx`
- `Frontend/src/pages/Login.tsx`
- `Frontend/src/pages/admin/AdminDashboard.tsx`
- `Frontend/src/App.tsx`

---

## Migrations Applied

| Migration | Description |
|-----------|-------------|
| `20260716142427_InitialCreate` | Создаёт полную схему БД — все 30 таблиц, FK, индексы (включая unique и soft-delete query filters на уровне модели) |
| `20260717051859_Migrations` | Пустая (Up/Down без операций) — создана без реальных изменений модели; кандидат на удаление/сквош при следующей уборке миграций |

---

## Architecture Decisions

| Decision | Reasoning |
|----------|-----------|
| Агрегации Analytics — в отдельном `IAnalyticsRepository` (Infrastructure), а не в `AnalyticsService` (Application) | `MarketTJ.Application` не ссылается на EF Core/Infrastructure (Clean Architecture) — агрегации на `GroupBy/Sum/Count` физически не могли жить в Application-сервисе; вынесены в репозиторий по тому же паттерну, что и все остальные |
| `IPasswordHasher`/`BCryptPasswordHasher` — интерфейс в Application, реализация в Infrastructure | `BCrypt.Net-Next` доступен только в Infrastructure (как и `ICacheService`/`RedisCacheService`) — Application не должен тянуть пакет напрямую |
| Auth без JWT — `LoginResponseDto` хранится на фронте как признак сессии | В проекте нет системы аутентификации (JWT/Identity — «Этап 2», раздел 23 ТЗ); чтобы дать пользователю реально войти и увидеть Admin-панель прямо сейчас, сделан минимальный проверочный логин без токена — временное решение, явно помечено TODO в `AuthController`/`AuthService`/`AuthContext` |
| Revenue в Analytics считается по `Order.Status == Completed`, а не по `Payment` | В проекте нет кода, который бы автоматически создавал `Payment` при доставке/завершении заказа (раздел 8.24 — задел на будущее) — агрегация по `Payment` всегда давала бы 0 при реальных продажах |
| Uniqueness/FK-проверки в сервисах — через `GetAllAsync()` + LINQ в памяти | Ни один из 30 репозиториев не имеет специализированных методов (`GetByEmailAsync` и т.п.) — правило «не выдумывай методы репозитория» применено последовательно во всех 30 сервисах |
| Только Admin-сценарий подключён на фронте в этой фазе | Явное указание пользователя — «сначала все шаги для админа»; Farmer/Customer/Courier кабинеты не начинались |

---

## Known Issues / Tech Debt

| Issue | Target Phase |
|-------|-------------|
| Нет JWT/Identity — `[Authorize(Roles=...)]` не применён ни на одном endpoint, все API открыты | Этап 2 |
| `AuthContext` хранит пользователя в `localStorage` без токена/подписи — не безопасно для продакшена | Этап 2 |
| Farmer/Customer/Courier кабинеты и логин-редиректы не реализованы на фронте (сейчас только заглушка-тост) | Этап 2–8 (по мере готовности каждой роли) |
| `AnalyticsController`: `farmerId` берётся из query-параметра, а не из claims — фермер теоретически может запросить чужие данные, пока нет Auth | Этап 2 |
| Нет полноценного checkout — `Order`/`OrderItem`/`Payment` независимые CRUD, сумма заказа не пересчитывается из состава корзины на сервере | Этап 5–6 |
| Рейтинг фермера (`FarmerProfile`) не пересчитывается при создании `Review` — поля `Rating` в `FarmerProfile` нет в модели | Этап 8 |
| Каталог (`ProductListing`) — только базовая серверная пагинация, нет поиска/фильтров по цене-региону-категории | Этап 4 |
| Миграция `20260717051859_Migrations` пустая — тех. долг, накопленный до этой фазы | Уборка перед Этапом 2 |
| Несовпадение значений `FarmerVerificationStatus`/`ListingStatus` в коде и в буквальном тексте ТЗ | Открытый вопрос, раздел 38 ТЗ — решение за пользователем |
| `DiscountRule` — сущность, на которую ссылались несколько внешних промптов-заданий, в модели отсутствует | Не начиналось |

---

## Test Coverage

| Suite | Tests | What's Covered |
|-------|-------|----------------|
| `MarketTJ.Application.Tests` (xUnit + Moq) | 743 | Все 30 `{Entity}Service`: Get/Create/Update/Delete, каждое правило валидатора отдельным тестом, FK/уникальность/бизнес-правила, обработка исключений репозитория. Интеграционных и E2E-тестов нет; `AnalyticsService`/`AuthService`/`AiAssistantService` unit-тестами не покрыты (не запрашивалось) |
| Frontend | 0 | Тестов нет; проверка — только ручная через браузер (реальный вход admin@market.tj → `/admin` → реальные данные из БД, logout, route guard) |

---

## Next Phase Overview

Следующий логичный шаг — закрыть Этап 2 по-настоящему: JWT/Identity, `[Authorize(Roles=...)]` на всех контроллерах,
`farmerId` из claims вместо query-параметра, безопасное хранение сессии на фронте вместо localStorage-объекта.
Это разблокирует Farmer/Customer/Courier кабинеты (Этапы 3–8), которые сейчас физически не могут быть защищены —
весь CRUD-слой и Analytics, построенные в этой фазе, уже готовы быть за ролевой авторизацией без переделок.
