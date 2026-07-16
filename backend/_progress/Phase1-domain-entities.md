# Phase 1 — Domain entities, enums, EF configurations, AppDbContext

## Что сделано

### Domain (backend/MarketTJ.Domain/)

**Enums/** (раздел 7 ТЗ, без изменений значений):
- FarmerVerificationStatus, ListingStatus, OrderStatus, DeliveryStatus

**Entities/** (раздел 8 ТЗ, только properties, без бизнес-логики, без EF-атрибутов):
- User, FarmerProfile, CustomerProfile, CourierProfile, Category, Product,
  ProductListing, ProductImage, CartItem, Order, OrderItem, Delivery, Review,
  Notification (14 файлов)

**Обновление**: во все 14 entity добавлены navigation properties для связей
из раздела 9 (1—1, 1—many, 1—0..1) — по прямой просьбе пользователя. Все 14
EF-конфигураций переписаны с `HasOne<T>().WithMany()` (без навигации) на
`HasOne(x => x.Nav).WithMany(x => x.Collection)` / `WithOne(x => x.Nav)`.
В схеме (раздел 9) **нет ни одной связи many-to-many** — визуально похожие
Customer↔ProductListing и Order↔ProductListing явно смоделированы через
собственные join-сущности с дополнительными данными (`CartItem.Quantity`,
`OrderItem.UnitPrice/Quantity/TotalPrice`), поэтому это два reference-навигейшена
с каждой стороны, а не EF skip-navigation many-to-many.

### Infrastructure (backend/MarketTJ.Infrastructure/)

**Persistence/Configurations/** — по одному `IEntityTypeConfiguration<T>` на
каждую сущность (14 файлов), Fluent API.

**Persistence/AppDbContext.cs** — DbSet на каждую сущность,
`ApplyConfigurationsFromAssembly` подхватывает все конфигурации автоматически.

**DependencyInjection.cs** — `AddInfrastructureServices(IConfiguration)`:
регистрирует `AppDbContext` с провайдером `Npgsql.EntityFrameworkCore.PostgreSQL`,
строка подключения — `ConnectionStrings:DefaultConnection`.

Пакеты: `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`.

### Application (backend/MarketTJ.Application/)

**DependencyInjection.cs** — `AddApplicationServices()`, пока пустой (нечего
регистрировать — сервисов/валидаторов ещё нет), оставлен для единообразного
вызова из Program.cs.

### WebApi (backend/MarketTJ.WebApi/)

**Program.cs** — переписан:
- вызывает `AddApplicationServices()` и `AddInfrastructureServices(configuration)`;
- создаёт `wwwroot/uploads/listings/` при старте (раздел 17);
- Swagger — заменён с минимального `AddOpenApi()`/`MapOpenApi()` (шаблон .NET 10)
  на классический `Swashbuckle.AspNetCore` (`AddSwaggerGen`/`UseSwaggerUI`) —
  даёт интерактивную страницу `/swagger`, а не только JSON-документ;
  JWT Bearer security definition НЕ добавлена — это Этап 2 (Authentication),
  сейчас нет ни одного `[Authorize]`-эндпоинта, описывать схему рано;
- при старте вызывается `context.Database.MigrateAsync()` — пока миграций нет,
  это no-op; когда появится первая миграция, будет применяться автоматически;
- `UseStaticFiles()` добавлен (для отдачи `/uploads/...`).

**appsettings.json** — добавлен `ConnectionStrings:DefaultConnection` с
плейсхолдером (`Password=CHANGE_ME`) — **не** реальные креды.

Пакеты: `Microsoft.EntityFrameworkCore.Design` (нужен для `dotnet ef` CLI),
`Swashbuckle.AspNetCore` (заменил `Microsoft.AspNetCore.OpenApi`).

## Проверено

- `dotnet build MarketTJ.slnx` — 0 ошибок, 0 предупреждений.
- `dotnet ef dbcontext info --project MarketTJ.Infrastructure --startup-project MarketTJ.WebApi`
  — контекст находится, провайдер и строка подключения читаются корректно.
  **Миграция НЕ создавалась** — по прямому указанию пользователя, это его
  следующий шаг: `dotnet ef migrations add InitialCreate --project MarketTJ.Infrastructure --startup-project MarketTJ.WebApi`.

## Решения, принятые самостоятельно (не додуманы — везде есть основание в ТЗ)

### Типы полей (раздел 8 даёт только имена свойств, не типы)
- `Id` и все foreign key — `int` (раздел 37.3, после правки пользователя с GUID на int).
- Денежные поля — `decimal`; в конфигурациях `HasPrecision(18, 2)` (раздел 18).
- Поля количества — `decimal`; `HasPrecision(18, 3)` (раздел 18).
- `Role` (User), `CustomerType` (CustomerProfile), `TransportType` (CourierProfile),
  `Unit` (Product) — оставлены как `string`, НЕ enum, т.к. в разделе 7 ТЗ для них
  enum не описан (там только 4 enum: FarmerVerificationStatus, ListingStatus,
  OrderStatus, DeliveryStatus). Если нужны — уточнить и добавить отдельно.
- `IsDeleted`/`DeletedAt` (раздел 18, soft delete) — **не добавлены** ни в одну
  сущность, т.к. они не перечислены в конкретном списке полей ни одной сущности
  в разделе 8, а раздел 18 помечает их как «рекомендуется», не обязательно.
  Если нужен soft delete — это отдельное решение, не додумывал сам.
- `CreatedAt`/`UpdatedAt` — добавлены только там, где явно перечислены в
  разделе 8 для конкретной сущности (например, `ProductImage`, `OrderItem`,
  `Review`, `Notification` — только `CreatedAt`, без `UpdatedAt`, как в списке).

### Nullable-поля (выведены из текста ограничений раздела 8/10/21, не придуманы)
- `FarmerProfile.VerifiedAt`, `VerifiedByAdminId` — nullable (заполняются только
  после подтверждения).
- `ProductListing.WholesalePricePerKg`, `WholesaleMinimumQuantity` — nullable
  (раздел 21: «больше 0 или null»). `HarvestDate`, `ExpectedHarvestDate`,
  `QualityGrade` — nullable, не упомянуты как обязательные.
- `Order.CustomerComment`, `AcceptedAt`, `CompletedAt`, `CancelledAt` — nullable
  (заполняются только при достижении соответствующего состояния).
- `Review.Comment` — nullable (раздел 10.6: «комментарий необязателен»).
- `Delivery.PickedUpAt`, `DeliveredAt` — nullable; `AssignedAt` — НЕ nullable
  (см. ниже).

### Поведение при удалении связей (раздел 9 даёт только тип связи 1—1/1—many/0..1,
без ON DELETE — решения по правилу «где неочевидно — Restrict», без выдумывания
дополнительной бизнес-логики):
- **Cascade**: User → FarmerProfile/CustomerProfile/CourierProfile (профиль
  неотделим от пользователя), User → Notification, ProductListing → ProductImage
  (раздел 17: удаление объявления удаляет его изображения), Order → OrderItem.
- **SetNull**: FarmerProfile.VerifiedByAdminId → User (поле nullable; удаление
  админа не должно ни блокироваться, ни стирать профиль фермера).
- **Restrict** (везде, где сущность — самостоятельная бизнес-запись/справочник,
  а не «деталь» родителя): Category→Product, Product→ProductListing,
  FarmerProfile→ProductListing, CustomerProfile/ProductListing→CartItem,
  CustomerProfile/FarmerProfile→Order, ProductListing→OrderItem, Order→Delivery,
  CourierProfile→Delivery, Order/FarmerProfile/CustomerProfile→Review.

  **Это решение стоит перепроверить**, когда появится реальная бизнес-логика
  удаления (например, может понадобиться SetNull для ProductListing в OrderItem,
  чтобы можно было удалять старые объявления, не трогая историю заказов).

### Delivery.CourierId — не nullable
Раздел 8.12 не помечает поле как nullable. Delivery создаётся действием
«Admin assigns Courier» (раздел 10.5/6.4) — то есть курьер известен уже в
момент создания строки, поэтому `CourierId` и `AssignedAt` сделаны обязательными
(не nullable). `DeliveryStatus.Pending` (раздел 7.4) в этой модели описывает
состояние заказа до появления Delivery, а не строку Delivery без курьера.

### Review.CustomerId — связь добавлена по аналогии
Раздел 9 явно перечисляет только `Order 1—0..1 Review` и
`FarmerProfile 1 — many Review`, но раздел 8.13 указывает поле `CustomerId` у
Review. Добавлена связь `Review.CustomerId → CustomerProfile.Id` — это заполнение
очевидного пробела между разделами 8 и 9, а не новое свойство.

### CartItem — уникальность (Customer, ProductListing)
Раздел 8.9: «один и тот же продукт не должен повторяться» — реализовано как
уникальный индекс `(CustomerId, ProductListingId)`, а не как новое поле.

## Осталось сделать дальше (Этап 1, раздел 23 ТЗ)

- `dotnet ef migrations add InitialCreate` — **пользователь делает сам**.
- Result pattern (Application/Common).
- Exception middleware (WebApi).
- Seed данных (роли, admin, категории — раздел 22) — Seeder ещё не создан,
  вызов из Program.cs намеренно не добавлен.
- JWT Bearer authentication — Этап 2 раздела 23, не начато намеренно.
