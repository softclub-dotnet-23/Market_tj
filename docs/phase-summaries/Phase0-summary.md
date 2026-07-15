# Phase 0 — Основа проекта (репозиторий и документация)

Статус: 🚧 In Progress

## What Was Built

- Структура репозитория: `backend/`, `frontend/`, `docs/`.
- `backend/`: solution `MarketTJ.slnx` + 5 проектов — `MarketTJ.Domain`, `MarketTJ.Application`, `MarketTJ.Application.Tests`, `MarketTJ.Infrastructure`, `MarketTJ.WebApi` — со ссылками по разделу 11 ТЗ (Application→Domain; Infrastructure→Domain+Application; WebApi→Application+Infrastructure; Application.Tests→Application). Только пустые папки (`Entities`, `Enums`, `Services`, `Interfaces/Repositories`, `Interfaces/Services`, `Common`, `Dto`, `Validators`, `Persistence/Repositories`, `Persistence/Configurations`, `Persistence/Seeder`, `Controllers`) — ни одной entity, DTO, контроллера или бизнес-логики. Демо-код шаблонов (`Class1.cs`, `WeatherForecastController.cs`, `UnitTest1.cs`) удалён. `dotnet build` проходит без ошибок.
- `frontend/`: React + TypeScript + Vite, только структура (`src/pages`, `src/components`, `src/layouts`, `src/api`, `src/auth`, `src/state`), без страниц и логики (`App.tsx` — пустой компонент). Node.js/npm не установлены в среде разработки — конфиги (`package.json`, `vite.config.ts`, `tsconfig*.json`) написаны вручную, `npm install` нужно будет выполнить перед первым запуском.
- `docs/`: заполнены реальным содержимым (перенесено/адаптировано из `TZ_MarketTJ_ClaudeCode.md`) — Vision, MVP, Roles, Database, DesignerLogic, Api, Frontend, StateMachines, ArchitectureRules, PROGRESS.
- В `TZ_MarketTJ_ClaudeCode.md` добавлены подпункты 37.1 (формат коммитов), 37.2 (шаблон phase-summary), 37.3 (шаблон Api.md) — их не было в исходном документе.

## Key Decisions

- Frontend — React + TypeScript (Vite), а не Blazor из раздела 12 ТЗ (явное указание пользователя).
- Репозиторий разделён на `backend/` и `frontend/` (в ТЗ изначально — плоская структура на уровне корня).
- `MarketTJ.Application.Tests` (xUnit) создан уже на Phase 0, а не отложен до Этапа 9.
- `.gitkeep` добавлены во все пустые папки, чтобы структура сохранилась в git.

## Known Issues / TODO

- Node.js/npm отсутствуют в среде — frontend не проверялся запуском (`npm install` / `npm run dev`).
- NuGet-предупреждение NU1903 (уязвимость в транзитивном пакете `Microsoft.OpenApi` 2.0.0, подтягивается через `Microsoft.AspNetCore.OpenApi` 10.0.10 в шаблоне Web API) — не устранялось, вне рамок Phase 0.
- `AppDbContext`, Identity, миграции, PostgreSQL-подключение — не созданы, это Этап 1 (раздел 23).

## Next Steps

- Дождаться подтверждения пользователя ("продолжай").
- Начать Этап 1 раздела 23: `AppDbContext`, базовые entity, migrations, Result pattern, exception middleware, Swagger.
