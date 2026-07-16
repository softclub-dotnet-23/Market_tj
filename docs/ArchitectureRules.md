# Architecture Rules

> Источник: TZ_MarketTJ_ClaudeCode.md (версия 1.1, финальная — React указан в стеке изначально, не как отклонение), разделы 11, 12, 16–19, 20, 20.1, 21, 37, 37.1–37.3.
> Это hard rules (раздел 0, пункт 6 ТЗ) — нарушать нельзя даже ради скорости.

## Архитектура решения

Монорепозиторий backend/frontend/docs:

```text
Market-tj/
├── backend/
│   ├── MarketTJ.Domain/
│   ├── MarketTJ.Application/
│   ├── MarketTJ.Application.Tests/
│   ├── MarketTJ.Infrastructure/
│   └── MarketTJ.WebApi/
├── frontend/          (React + TypeScript)
└── docs/
```

### MarketTJ.Domain

Только `Entities/`, `Enums/`. Не зависит от других проектов.

### MarketTJ.Application

```text
Services/
Interfaces/Repositories/    ← I...Repository
Interfaces/Services/         ← I...Service
Common/                       ← PagedResult, ErrorType/коды ошибок
Dto/                            ← подпапка на каждую сущность/фичу (AuthDto, FarmerDto, ListingDto, OrderDto, DeliveryDto, ReviewDto, NotificationDto...)
Validators/                      ← подпапки зеркалят Dto/, один файл на один сценарий (RegisterFarmerValidator, CreateListingValidator...), не один файл на весь модуль
```

Зависит только от Domain. Новая сущность/фича → сразу своя подпапка в `Dto/`, не сваливать всё в одну папку.

### MarketTJ.Application.Tests

`Services/` — unit-тесты, один класс тестов на сервис (`AuthServiceTests`, `OrderServiceTests`...). Зависит от Application.

### MarketTJ.Infrastructure

```text
Persistence/
├── AppDbContext.cs
├── Repositories/       ← реализации I...Repository
├── Configurations/     ← EntityTypeConfiguration на каждую сущность
└── Seeder/             ← роли, admin, категории (раздел 22)
```

Зависит от Domain и Application. `Migrations/` — генерируются EF Core внутри `Persistence/`. Файловое хранилище (`IFileStorageService`) и уведомления — отдельные папки рядом с `Persistence/` (например `FileStorage/`, `Notifications/`), не смешивать с Persistence.

### MarketTJ.WebApi

`Controllers/` — все контроллеры. Middleware/фильтры/авторизация/`Program.cs` — на уровне проекта, вне `Controllers/`. Зависит от Application и Infrastructure.

**Контроллеры не должны содержать бизнес-логику** — только вызов сервисов из Application.

### frontend (React)

```text
src/
├── pages/
│   ├── public/    ← Public/Customer Website
│   ├── farmer/    ← Farmer Panel
│   ├── admin/     ← Admin Panel
│   └── courier/   ← Courier mini-interface (2 страницы)
├── components/
├── layouts/       ← отдельный layout на каждую из 3 частей; у courier/ своего layout с меню нет
├── api/           ← клиенты к backend API
├── auth/          ← токен, защищённые роуты
└── state/         ← глобальное состояние (например корзина)
```

React + TypeScript, React Router, Axios/fetch, состояние — React Context (Redux/Zustand не обязателен для MVP). Стилизация — на усмотрение frontend-разработчика (Tailwind/CSS Modules/Bootstrap), если не оговорено отдельно.

Взаимодействие с backend только через HTTP API:

```text
React → HTTP API → Application → Infrastructure → PostgreSQL
```

**Frontend не должен напрямую обращаться к базе данных** — архитектурно невозможно (разные процессы), только через Web API.

## Технологии

Backend:

* .NET 10, ASP.NET Core Web API, Entity Framework Core, PostgreSQL, ASP.NET Core Identity, JWT Bearer, FluentValidation, AutoMapper/ручной mapping, Swagger, Serilog.

Frontend:

* React, TypeScript, React Router, Axios/fetch, React Context, localStorage/защищённое хранилище токена, адаптивный дизайн.

Тестирование:

* xUnit, FluentAssertions, интеграционные тесты для основных API; Frontend-тесты (Vitest/RTL) не обязательны для MVP, но приветствуются для корзины/чекаута.

## Безопасность

Обязательные требования:

* пароли хранить через Identity;
* JWT должен содержать UserId и Role;
* endpoints защищать через `[Authorize]`;
* операции ролей защищать через `[Authorize(Roles = "...")]`;
* проверять владельца сущности;
* не принимать UserId из клиента, если его можно получить из токена;
* валидировать DTO;
* ограничивать размер файлов;
* проверять расширение файла;
* использовать HTTPS;
* не возвращать PasswordHash;
* не хранить секреты в Git;
* использовать глобальный exception middleware;
* логировать критические ошибки;
* использовать pagination;
* защищаться от отрицательных цен и количества.

Дополнительно (рекомендуется, не блокирует Definition of Done):

* короткоживущий access token (JWT) + refresh token;
* rate limiting на `/api/auth/login` и `/api/auth/register/*`.

## Работа с файлами

Файлы хранятся: `wwwroot/uploads/listings/{listingId}/`. В базе — только путь, например `/uploads/listings/15/image-1.jpg`.

```text
Разрешённые форматы: jpg, jpeg, png, webp
Максимальный размер: 5 MB
Максимум изображений: 5
```

При удалении объявления связанные изображения удаляются или помечаются удалёнными. Доступ к файлам — через `IFileStorageService`, не напрямую через `File.Save` (задел на облачное S3-совместимое хранилище).

## Общие требования к данным

* **IDs: int** (autoincrement, раздел 37.3 — исправлено пользователем с GUID на int) — для всех сущностей;
* денежные значения — `decimal`, `HasPrecision(18, 2)`;
* количество — `decimal`, `HasPrecision(18, 3)`;
* даты — ISO 8601 UTC;
* общие поля сущностей: `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`;
* для важных данных рекомендуется soft delete.

## Нефункциональные требования

* время ответа API для обычных запросов — до 2 секунд;
* каталог должен использовать pagination;
* приложение должно корректно работать на телефоне;
* ошибки должны иметь единый формат;
* Swagger должен открываться без ошибок;
* база данных должна создаваться через migrations;
* проект должен запускаться после настройки connection string;
* код должен быть разделён по слоям;
* контроллеры не должны содержать бизнес-логику;
* frontend не должен напрямую обращаться к базе данных.

## Формат ответа API и коды ошибок

См. [Api.md](Api.md) — единый документ конвенций (формат ответа, HTTP-коды, `errorCode`, пагинация, int ID, endpoint catalogue). JSON camelCase, DateTimes ISO 8601 UTC.

## Валидация

Register:

```text
FullName: обязательный, 3–100 символов
Email: обязательный, корректный
PhoneNumber: обязательный
Password: минимум 6 символов
```

ProductListing:

```text
Title: обязательный, 3–150 символов
Description: максимум 2000 символов
RetailPricePerKg: больше 0
WholesalePricePerKg: больше 0 или null
AvailableQuantity: больше 0
MinimumOrderQuantity: больше 0
```

Order:

```text
DeliveryAddress: обязательный
Cart: не пустой
Quantity: доступно на складе
Customer: активный
Farmer: подтверждённый
```

## Дополнительные рекомендации (раздел 37 ТЗ)

Не входят в обязательный Definition of Done, но снижают риски переделок:

1. Refresh token (короткоживущий JWT + отдельный refresh token) — не реализовано.
2. Rate limiting на auth-endpoints — не реализовано.
3. `IFileStorageService` вместо прямой работы с диском — не реализовано.
4. Машинно-читаемые `errorCode` вместо хардкода строк — не реализовано (формат — [Api.md](Api.md)).
5. Idempotency при создании заказа — не реализовано.

## Конвенции процесса (разделы 37.1–37.3)

### Формат коммитов (Phase → Step)

```text
Phase{N} Step {M} [BE|FE|FULL]: краткое описание сделанного
```

`BE` — только backend, `FE` — только frontend, `FULL` — оба. Примеры:

```text
Phase 0 Step 1 [FULL]: Repository scaffold
Phase 4 Step 7 [FE]: date range filter — presets + custom period
```

Если непонятно, к какому Phase/Step относится задача — сначала уточнить у пользователя, не придумывать нумерацию самостоятельно.

### Шаблон phase-summary (`docs/phase-summaries/Phase{N}-summary.md`)

```markdown
# Phase N — <Название> Summary

**Status:** ✅ Complete / 🚧 In Progress
**Completed:** YYYY-MM-DD
**Branch:** ...
**Tag:** vX.Y-название

## What Was Built
### Backend
| Item | Description |
### Frontend
| Item | Description |

## Key Files
## Migrations Applied
## Architecture Decisions
| Decision | Reasoning |
## Known Issues / Tech Debt
## Test Coverage
## Next Phase Overview
```

Пустые секции недопустимы — если пункта нет, писать «—». Таблица «Architecture Decisions» обязательна даже для маленьких этапов.

### Формат Api.md

`docs/Api.md` — документ конвенций (Base URL, JSON/camelCase, DateTimes, Money, IDs, Pagination, HTTP-коды, endpoint catalogue по группам из раздела 13), не просто список endpoint. Актуальная версия — [Api.md](Api.md).
