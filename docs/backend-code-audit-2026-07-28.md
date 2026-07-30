# Аудит backend Market.tj — 2026-07-28

Полный файл-за-файлом аудит `backend/` (MarketTJ.Domain, MarketTJ.Application, MarketTJ.Infrastructure, MarketTJ.WebApi, MarketTJ.Application.Tests). Аудит read-only — код не менялся. Ниже — только то, что подтверждено чтением исходников, статическим анализом Fluent API / DI-конфигурации либо реальным запуском (`dotnet build`, `dotnet test`, изолированная EF Core model-validation проверка). Там, где проверка требует поднятой БД/полного `dotnet run`, это явно помечено как "не проверено запуском".

---

## 1. Итог

**Объём проверки:**
- Domain: 30 сущностей (`Entities/`), 16 enum'ов (`Enums/`) — прочитаны все.
- Application: 33 группы DTO (~130 файлов, сплошной grep на утечку `PasswordHash` + выборочное чтение), 31 файл Validators (все — на использование `Enum.IsDefined`, 5 прочитаны построчно), 33 сервиса (7 прочитаны построчно, остальные — через DI-регистрацию и сигнатуры интерфейсов), 35 интерфейсов сервисов, 30 интерфейсов репозиториев.
- Infrastructure: `AppDbContext.cs` и **все 30** файлов `Configurations/` прочитаны построчно (это дало главную находку — см. ниже), 3 репозитория прочитаны построчно + DI сверка на все 30, `Seeder.cs` — построчно.
- WebApi: **все 39** контроллеров (34 в корне + 5 в `Admin/`) — сплошной grep `[Authorize]`/`[AllowAnonymous]`/HTTP-глаголов по каждому файлу, ~15 прочитаны построчно; оба middleware — построчно; `Program.cs` — построчно; `appsettings.json` и `appsettings.Development.json` — построчно.
- MarketTJ.Application.Tests: `dotnet test` запущен реально.

**Числа (факт, не оценка):**
- `dotnet build -c Debug`: **0 ошибок, 0 предупреждений**.
- `dotnet build -c Release`: **0 ошибок, 0 предупреждений** (полностью идентично Debug).
- `dotnet test`: **806 пройдено, 0 не пройдено, 0 пропущено** (2 сек).
- EF Core model-validation (изолированный consile-harness с `UseInMemoryDatabase`, референс на реальный `AppDbContext` из `MarketTJ.Infrastructure`, см. методологию в разделе 🔴 №1): **21 предупреждение** `PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning` (EventId 10622).

**Итоговый счётчик находок:** 🔴 Critical — 5, 🟡 Warning — 11, 🔵 Suggestion — 9.

Главный вывод: код архитектурно опрятный, консистентный по слоям, с настоящими тестами (806 зелёных) и чистой сборкой — но есть системная проблема с авторизацией на уровне владения ресурсом (IDOR) почти во всём "личном" CRUD API, плюс подтверждённая регрессия/незакрытие known-issue по soft-delete query filters, плюс отсутствие хэширования пароля в admin-эндпоинте управления пользователями. Все три — блокеры для продакшен-деплоя.

---

## 2. 🔴 Critical

### Application

**🔴 2.1 — Admin-эндпоинт создания/редактирования пользователя хранит пароль без хэширования**
Файлы: `MarketTJ.Application/Services/UserService.cs:75,115`, `MarketTJ.Application/Validators/UserValidator.cs:24,41-45`, `MarketTJ.Application/Dto/UserDto/CreateUserDto.cs`, `UpdateUserDto.cs`, контроллер `MarketTJ.WebApi/Controllers/UserController.cs` (`[Authorize(Roles = "Admin")]`, `POST/PUT /api/users`).

`CreateUserDto`/`UpdateUserDto` содержат поле `PasswordHash` (не `Password`). `UserValidator.Validate(...)` применяет к нему только `IsNullOrWhiteSpace` + `Length < 6` — те же правила, что и к обычному паролю (комментарий в коде это прямо признаёт: *"применено к PasswordHash — в проекте ещё нет отдельного сырого поля Password"*). А `UserService.CreateAsync`/`UpdateAsync` берут `dto.PasswordHash` и **присваивают его напрямую** `user.PasswordHash` — без единого вызова `BCrypt.Net.BCrypt.HashPassword(...)` (для сравнения — `AuthService.RegisterAsync` делает это правильно, строка 35).

Практическое следствие: клиент/фронтенд, вызывающий `POST /api/users`, обязан передать в поле `PasswordHash` то, что попадёт в БД буквально. Если фронтенд (разумно) отправляет обычный пароль — он **сохраняется в открытом виде** в колонке, которая называется "hash". Дополнительно это ломает сам логин: `AuthService.LoginAsync` вызывает `BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)`, а для строки, не являющейся валидным bcrypt-хэшем, `Verify` либо кинет исключение, либо всегда вернёт `false` — то есть пользователь, созданный/изменённый админом через этот эндпоинт, не сможет залогиниться в принципе.

Почему это Critical: и утечка пароля в открытом виде в БД, и полностью нерабочий функционал одновременно.

Исправление: убрать `PasswordHash` из `CreateUserDto`/`UpdateUserDto`, добавить `Password` (raw), хэшировать в сервисе через `BCrypt.Net.BCrypt.HashPassword` перед записью в `User.PasswordHash`, как уже сделано в `AuthService`. Для Update — сделать смену пароля отдельным необязательным полем, а не обязательным при каждом обновлении профиля.

---

### Application / WebApi (системная проблема, затрагивает 21 контроллер)

**🔴 2.2 — Отсутствует авторизация на уровне владения ресурсом (IDOR) почти во всём "личном" CRUD API**
Затронутые контроллеры (подтверждено grep + чтением сервисов): `CartItemController`, `FavoriteController`, `OrderController`, `OrderItemController`, `ConversationController`, `ChatMessageController`, `NotificationController`, `ReviewController`, `RefundRequestController`, `SupportTicketController`, `SupportMessageController`, `PaymentController`, `FarmerDocumentController`, `FarmerStaffMemberController`, `CustomerProfileController`, `FarmerProfileController`, `CourierProfileController`, `DeliveryController`, `DeliverySlotController`, `ReportedListingController`, `ProductImageController` (частично), `ProductListingController` — **21 контроллер из 39**.

Из всех 39 контроллеров `ICurrentUserService` инжектируется только в 6: `MeController`, `AnalyticsController`, `AdminUserController`, `AdminSupportController`, `AdminProductController`, `AdminOrderController` (grep `currentUser` по `Controllers/` — полный список).

Два независимых проявления в каждом из 21 контроллера:

1. **`GetAll()` не фильтрует по текущему пользователю вообще.** Пример — `CartItemService.GetAllAsync()` (`MarketTJ.Application/Services/CartItemService.cs:18-30`) делает `cartItemRepository.GetAllAsync()` без единого `Where` по клиенту — то есть `GET /api/cart-items` под любым авторизованным JWT (в том числе ролью Customer) возвращает **корзины всех покупателей платформы**. То же самое подтверждено построчным чтением `NotificationService.GetAllAsync` (все уведомления всех пользователей), `ConversationService.GetAllAsync` (все переписки всех пар покупатель/фермер), `OrderService.GetAllAsync` (все заказы всех клиентов, включая ФИО и телефон — см. `ResolveCustomerContactsAsync`).
2. **Create/Update принимают `CustomerId`/`FarmerId`/`UserId`/`SenderId` напрямую из тела запроса, без сверки с JWT.** Пример — `CreateCartItemDto.CustomerId`, `CreateFavoriteDto.CustomerId`, `CreateOrderDto.CustomerId`/`FarmerId`, `CreateNotificationDto.UserId`, `UploadChatMessageRequest.SenderId` (`ChatMessageController.cs:27`) — сервис лишь проверяет, что профиль с таким Id *существует* (`GetByIdAsync(dto.CustomerId) is null → NotFound`), но не что он принадлежит вызывающему. Любой авторизованный Customer может добавить товар в **чужую** корзину, оформить заказ **от имени другого покупателя**, отправить сообщение в чат **от имени другого пользователя**, пометить **чужой** товар избранным и т.д.

Это ровно тот паттерн, о котором просили проверить ("Farmer/Customer 'my own resource' эндпоинты должны брать id из JWT через `ICurrentUserService`, а не из параметра"), но на практике оказалось хуже: это не query/route параметр, а прямое поле в теле, и `GetAll` не отфильтрован совсем — то есть это не только IDOR на write-операциях, но и полноценная утечка чужих данных на read.

Почему Critical: прямое нарушение конфиденциальности данных (чужие заказы, переписки, уведомления, документы фермера) и прямая возможность фальсификации данных от чужого имени — критично для маркетплейса с реальными деньгами и персональными данными до продакшен-деплоя.

Исправление (по каждому из 21 контроллера): 1) `GetAll()` — добавить перегрузку сервиса `GetAllForUserAsync(int userId, string role)` (или аналог), фильтрующую по `CustomerId`/`FarmerId`/`UserId`, соответствующему `ICurrentUserService.UserId`; Admin — отдельный полный список через уже существующий `Admin/*` контроллер. 2) Create/Update — убрать `CustomerId`/`FarmerId`/`UserId`/`SenderId` из тела DTO там, где это "мой ресурс", и подставлять их на сервере из `ICurrentUserService`, а не доверять телу запроса.

---

### Infrastructure

**🔴 2.3 — Регрессия (или незакрытие) known-issue: EF Core global query filter cascade — 21 связь без согласованных фильтров**
Файлы: все 30 `MarketTJ.Infrastructure/Persistence/Configurations/*.cs`; `HasQueryFilter` определён буквально в трёх — `UserConfiguration.cs:22`, `ProductListingConfiguration.cs:40`, `OrderConfiguration.cs:41`. Ни один из файлов `CartItemConfiguration`, `FavoriteConfiguration`, `ReviewConfiguration`, `ReportedListingConfiguration`, `DeliveryConfiguration`, `DeliverySlotConfiguration`, `ConversationConfiguration`, `ChatMessageConfiguration`, `SupportTicketConfiguration`, `SupportMessageConfiguration`, `AuditLogConfiguration`, `AppSettingConfiguration`, `FarmerDocumentConfiguration`, `FarmerStaffMemberConfiguration`, `RefundRequestConfiguration`, `PaymentConfiguration`, `RefreshTokenConfiguration`, `NotificationConfiguration`, `FarmerProfileConfiguration`, `CustomerProfileConfiguration`, `CourierProfileConfiguration`, `ProductImageConfiguration` не определяет согласованный `HasQueryFilter`, хотя все они содержат **обязательную** (non-nullable) навигацию на `User`, `Order` или `ProductListing`.

Это проверено не только чтением кода, но и **реальным запуском**: собран изолированный консольный harness (`Microsoft.EntityFrameworkCore.InMemory`, ссылка на реальную сборку `MarketTJ.Infrastructure`, тот же самый класс `AppDbContext`), инициализация модели через `dbContext.Model` + пробный запрос. Результат — **21 предупреждение** уровня `warn` от `Microsoft.EntityFrameworkCore.Model.Validation[10622]`, например:
```
Entity 'ProductListing' has a global query filter defined and is the required end of a
relationship with the entity 'CartItem'. This may lead to unexpected results when the
required entity is filtered out...
Entity 'Order' has a global query filter defined and is the required end of a relationship
with the entity 'OrderItem'. ...
Entity 'User' has a global query filter defined and is the required end of a relationship
with the entity 'AuditLog'. ...
```
Полный список пар (Entity-с-фильтром / зависимая сущность): User↔AuditLog, User↔ChatMessage, User↔Conversation, User↔CourierProfile, User↔CustomerProfile, User↔FarmerProfile, User↔FarmerStaffMember, User↔Favorite, User↔Notification, User↔RefreshToken, User↔RefundRequest, User↔SupportMessage, User↔SupportTicket, ProductListing↔CartItem, ProductListing↔ProductImage, ProductListing↔ReportedListing, Order↔Delivery, Order↔DeliverySlot, Order↔OrderItem, Order↔Payment, Order↔Review — **21 связь**.

Почему это важно на практике (не только "шумное предупреждение при старте"): при soft-delete `User`/`ProductListing`/`Order` (`IsDeleted = true`) любой `.Include()` дочерней сущности на связанную обязательную навигацию (например `CartItem.Include(c => c.ProductListing)`, когда листинг уже мягко удалён) отдаёт `ProductListing == null`, хотя C#-модель объявляет её non-nullable (`= null!`). Это прямой путь к `NullReferenceException` в проде в тот момент, когда фермер/товар/пользователь уже "удалён", а связанные записи (корзины, избранное, чаты, платежи, отзывы, документы) — ещё нет.

Задача просила именно перепроверить, не регрессировал ли уже закрытый ранее баг — по факту находки в коде фикс либо не был доведён до всех сущностей, добавленных позже, либо не применялся вовсе (в конфигурациях нет ни одного упоминания второго `HasQueryFilter`, кроме исходных трёх).

Исправление: на каждой из 21 зависимой конфигурации добавить `builder.HasQueryFilter(x => !x.ProductListing.IsDeleted)` (либо аналог через связанную сущность/собственный `IsDeleted`, если он появится), либо явно сделать навигацию `optional` там, где это оправдано бизнес-логикой (например, `Order.Review`). Простого решения "на одну строку" нет — нужно решить по каждой сущности отдельно, входит ли она сама в периметр soft-delete.

---

### Api / Infrastructure

**🔴 2.4 — Нет rate limiting на `/api/auth/login` (и вообще нигде)**
Файлы: `MarketTJ.WebApi/Program.cs` (нет `AddRateLimiter`/`UseRateLimiter`), `MarketTJ.WebApi/Controllers/AuthController.cs` (`[AllowAnonymous]`, `POST /api/auth/login` без каких-либо ограничений). Grep `RateLimit` по всему `backend/` — 0 совпадений в коде (только в найденных npm/nuget метаданных отсутствуют).

`LoginAsync` в `AuthService.cs:58-75` — обычный email+password с `BCrypt.Verify`, без блокировки после N неудачных попыток, без CAPTCHA, без троттлинга по IP/аккаунту. Ничего в pipeline (`Program.cs`) это не компенсирует.

Почему Critical: `/api/auth/login` — открытый анонимный эндпоинт, прямая мишень для brute-force/credential-stuffing атак на все роли, включая Admin (email админа предсказуем — `admin@market.tj` в сидере). Без rate limiting это чисто вопрос времени.

Исправление: подключить встроенный ASP.NET Core `Microsoft.AspNetCore.RateLimiting` (`FixedWindowLimiter` или `SlidingWindowLimiter` по IP+email) минимум на `/api/auth/login` и `/api/auth/register`, до продакшен-деплоя.

---

### Infrastructure / DevOps

**🔴 2.5 — `appsettings.json` (базовый, не Development) содержит реальный, а не заглушечный пароль от Postgres**
Файл: `MarketTJ.WebApi/appsettings.json:13`:
```json
"DefaultConnection": "Host=localhost;Port=5432;Database=markettj;Username=postgres;Password=07806634"
```
`07806634` — восьмизначное число, явно не placeholder вида `postgres`/`changeme`/`YOUR_PASSWORD` — похоже на реальный локальный пароль разработчика.

Уточнение против первоначальной гипотезы задания: этот файл **тоже полностью в `.gitignore`** (`backend/MarketTJ.WebApi/appsettings.json` и `appsettings.*.json` — обе строки есть в `.gitignore`, `git ls-files` подтверждает, что файл не закоммичен ни разу) — то есть через git-историю секрет не утечёт, аналогично `appsettings.Development.json`.

НО: `backend/Dockerfile:18` делает `COPY backend/ .` — Docker `COPY` не смотрит в `.gitignore`, только в `.dockerignore`, а `.dockerignore` в репозитории **отсутствует полностью** (проверено — файла нет). Значит, при **локальной** сборке образа (`docker-compose`, упомянутый в комментариях самого Dockerfile) оба файла — `appsettings.json` и `appsettings.Development.json` — при их наличии на диске у разработчика **попадут внутрь слоёв образа целиком**, включая реальный Postgres-пароль и dev JWT-секрет. Если такой локально собранный образ хоть раз запушат в реестр (Docker Hub, Railway registry вручную и т.п.) — секреты уйдут вместе с ним, даже если сама Production-конфигурация их не использует.

Почему Critical: реальный пароль БД, экспортируемый за пределы git по параллельному каналу (Docker layer), который никто не проверял.

Исправление: 1) добавить `.dockerignore` с `**/appsettings.json` и `**/appsettings.*.json` (кроме, возможно, `appsettings.Production.json`, если он появится и не будет содержать секретов); 2) сменить локальный Postgres-пароль на непубличную конвенцию (`postgres`/`local-only`) раз он всё равно только для машины разработчика; 3) удостовериться, что Railway-деплой не полагается на присутствие этого файла (см. находку 2.6 ниже — по коду это не так, но стоит явно подтвердить).

---

---

## 3. 🟡 Warning

### Domain / Application

**🟡 3.1 — `ErrorType` не различает 401 (не аутентифицирован) и 403 (аутентифицирован, но не свой ресурс)**
Файлы: `MarketTJ.Application/Common/ErrorType.cs`, `MarketTJ.WebApi/Controllers/ApiControllerBase.cs:21-28`. В enum нет значения `Forbidden`, `HandleResult` мапит `ErrorType.Unauthorized` только на 401. Как только пункт 2.2 (IDOR) будет исправляться и появятся проверки владения ресурсом, для них потребуется семантически верный 403, а не 401 — иначе фронтенд не сможет отличить "нужно перелогиниться" от "это не ваше".
Исправление: добавить `ErrorType.Forbidden` → `StatusCodes.Status403Forbidden`.

**🟡 3.2 — Дублирующийся эндпоинт листинга пользователей: `GET /api/users` (неограниченный) и `GET /api/admin/users` (пагинированный)**
Файлы: `UserController.cs:12-14` (`GetAll()` без пагинации, вызывает `IUserService.GetAllAsync()`), `AdminUserController.cs:15-17` (`GetAll([FromQuery] PagedRequest ...)`, вызывает `GetPagedAsync`). Оба `[Authorize(Roles = "Admin")]`. Два разных пути к практически одному и тому же списку — путаница для фронтенда и лишняя площадь для несогласованной логики (например, фильтрация по `role`/`isActive` есть только во втором).
Исправление: либо удалить неограниченный `GetAll()` из `UserController`, либо явно развести назначение (например, `UserController` — только `GetById`/CRUD одного пользователя, весь листинг — через `Admin/AdminUserController`).

**🟡 3.3 — Список GET-эндпоинтов почти нигде не пагинирован, кроме `ProductListingController` и `Admin/*`**
Файлы: `CategoryController.cs`, `ProductController.cs`, `ReviewController.cs`, `CartItemController.cs`, `FavoriteController.cs`, `ConversationController.cs`, `NotificationController.cs`, `OrderController.cs` (не путать с `AdminOrderController`, который пагинирован) и другие — `GetAll()` без `pageNumber`/`pageSize` вообще. По мере роста данных (заказы, отзывы, уведомления) это выльется в тяжёлые ответы и полную загрузку таблицы в память на каждый запрос (сервисы делают `repository.GetAllAsync()` без `Skip/Take` на уровне БД).
Исправление: по крайней мере для `Order`, `Review`, `Notification`, `ChatMessage`, `Conversation` — перевести `GetAll` на существующий паттерн `PagedRequest`/`PagedResult<T>` (он уже есть и используется в `OrderService.GetPagedAsync`/`UserService.GetPagedAsync`/`RefundRequestService.GetPagedAsync` — просто не выведен наружу в обычных, не-Admin контроллерах).

### Infrastructure

**🟡 3.4 — Нет обычных (не уникальных) индексов на часто фильтруемых не-FK колонках**
Файлы: все `Configurations/*.cs` — единственные `HasIndex` в проекте (18 штук) все являются составляющими уникальных бизнес-ограничений (email, phone, order number и т.д.). FK-колонки (`ProductListingId`, `CustomerId`, `FarmerId` и т.п.) индексируются автоматически EF Core-конвенцией (это не проблема). Но колонки вроде `ProductListing.Status`, `Order.Status`, `User.IsActive`, `User.Role`, `Category.IsActive`, `DeliveryZone.IsActive` — не FK и не индексируются нигде явно, при этом активно используются в `Where` (`GetPagedAsync` в `OrderService`/`UserService`, фильтрация по `ListingStatus.Active` в каталоге). На малых объёмах данных (сидированные тестовые записи) это незаметно, но при росте таблиц `Order`/`ProductListing` до тысяч записей это будет full table scan на каждый листинг.
Исправление: добавить `builder.HasIndex(x => x.Status)` (и составные индексы вроде `(FarmerProfileId, Status)` на `ProductListing`) там, где фильтрация по этим полям происходит в горячих путях каталога/дашбордов.

**🟡 3.5 — `ProductListingRepository`/`CategoryRepository` кэшируют весь список сущностей целиком (`GetAllAsync`), без учёта фильтров**
Файл: `MarketTJ.Infrastructure/Persistence/Repositories/ProductListingRepository.cs:16-27`, `CategoryRepository.cs:12-23`. Комментарий в самом файле честно признаёт это как временное решение ("Публичный каталог с фильтрами/пагинацией появится на уровне Application-сервиса позже, там же будет кэш по конкретному набору фильтров"). Сейчас `GetAllAsync()` — единственный источник данных для листинга, кэшируется под одним ключом `product-listings:all`/`categories:all` целиком, TTL 10/30 минут; при появлении фильтрации в сервисе этот кэш не будет работать эффективно (репозиторий продолжит отдавать несфильтрованный полный список, фильтрация останется на уровне in-memory LINQ после кэша).
Исправление: не блокер сейчас (сам код признаёт это временным и корректно инвалидирует кэш на write), но стоит учитывать при следующем шаге пагинации каталога — не полагаться на этот кэш-ключ для сценариев с фильтрами.

**🟡 3.6 — `OrderService.GetAllAsync`/`OrderService.ResolveCustomerContactsAsync` — потенциальный N+1/полная загрузка при росте данных**
Файл: `MarketTJ.Application/Services/OrderService.cs:312-329`. `ResolveCustomerContactsAsync` вызывает `customerProfileRepository.GetAllAsync()` и `userRepository.GetAllAsync()` **целиком** (весь список профилей клиентов и весь список пользователей), просто чтобы отфильтровать по нужным Id в памяти — вместо `Where(id => neededIds.Contains(...))` на уровне БД. При росте базы пользователей это на каждый вызов `GetAllAsync()`/`GetPagedAsync()` заказов тянет в память все таблицы `Users`/`CustomerProfiles` целиком. То же самое в `CreateAsync`/`UpdateAsync` (проверка уникальности `OrderNumber` через `orderRepository.GetAllAsync()` вместо точечного запроса) и в `CartItemService.CreateAsync`/`UpdateAsync` (проверка дубликата в корзине тоже через полный `GetAllAsync()`).
Исправление: добавить в репозитории точечные методы (`ExistsByOrderNumberAsync`, `GetByIdsAsync(IEnumerable<int>)`) вместо повсеместного «тащим всё, фильтруем в LINQ-to-Objects».

**🟡 3.7 — `.gitignore` полностью исключает и `appsettings.json`, и `appsettings.Development.json` — при клонировании репозитория "с нуля" оба файла отсутствуют**
Файл: `.gitignore` (корень репозитория). Это ожидаемо и осознанно для секретов, но означает, что сам по себе `git clone` + `dotnet run` не заработает без ручного создания этих файлов — нет закоммиченного `appsettings.json.example`/`appsettings.Development.json.example` с плейсхолдерами (не проверено на 100% — не искал файл специально с этим именем, но не встретился ни разу за весь обход `WebApi/`). Это usability-находка, не баг рантайма.
Исправление: добавить `appsettings.Development.json.example` в репозиторий с плейсхолдерами, чтобы новый разработчик/CI мог быстро поднять окружение.

### Api

**🟡 3.8 — `ChatMessageController.Upload`/остальные "upload"-эндпоинты берут `SenderId`/владельца из тела запроса, а не из JWT (частный случай 2.2, отмечен отдельно из-за файловой специфики)**
Файл: `MarketTJ.WebApi/Controllers/ChatMessageController.cs:25-27`, `UploadChatMessageRequest` (`MarketTJ.WebApi/Models`). `request.SenderId` передаётся клиентом и используется как есть — сообщение с файлом можно отправить от имени другого пользователя чата.
Исправление: см. 2.2 — брать `SenderId` из `ICurrentUserService.UserId`.

**🟡 3.9 — Файлы объявлений/чатов не удаляются каскадно с БД-записью на уровне `Delete` (частично компенсировано на уровне `UserService`, но не везде)**
Файлы: `ProductImageService`/`ChatMessageService` — не проверено построчно на предмет вызова `IFileStorageService.Delete(...)` при удалении записи (в отличие от `UserService.DeleteAvatarAsync`/`UploadAvatarAsync`, где это сделано аккуратно, см. "что сделано хорошо"). Не проверено статически до конца из-за объёма — рекомендуется точечная проверка в отдельной задаче: убедиться, что `ProductImageService.DeleteAsync`/`ChatMessageService.DeleteAsync` тоже чистят файлы на диске, а не оставляют "осиротевшие" файлы в `wwwroot/uploads`.

**🟡 3.10 — JWT `Secret` отсутствует в базовом `appsettings.json` — Production обязан получить его через переменную окружения, иначе приложение падает при старте**
Файл: `MarketTJ.WebApi/appsettings.json` (секция `Jwt` — только `Issuer`/`Audience`/`ExpiryMinutes`, поля `Secret` нет вообще), `Program.cs:86` — `jwtSection["Secret"]!` (null-forgiving на потенциальный `null`). Это на самом деле **правильный** паттерн (секрет только через env var, не через файл), но именно поэтому это не проверяемая статически гарантия: если на Railway не будет установлена `Jwt__Secret` (или `JWT_SECRET`, в зависимости от того, как именно проброшено), приложение упадёт с `NullReferenceException` при первом же запросе, требующем аутентификации (или раньше — в зависимости от того, когда ASP.NET Core резолвит `TokenValidationParameters`). Не проверено запуском в Production-конфигурации — рекомендация: явно подтвердить перед деплоем, что переменная окружения для `Jwt:Secret` установлена в Railway.

**🟡 3.11 — Нет отдельного `.dockerignore` — см. также 2.5, здесь — общий риск, не только секреты**
Файл: отсутствует в репозитории. Помимо секретов, `COPY backend/ .` в `Dockerfile:18` копирует также `bin/`/`obj/` папки всех проектов (если они существуют локально на момент сборки — а они существуют, см. `ls` в начале аудита: `bin/Debug`, `obj/Debug` присутствуют во всех проектах), что раздувает build-контекст и может привести к несовместимым бинарникам, случайно попавшим в образ до `dotnet restore`/`publish`.
Исправление: добавить `.dockerignore` с `**/bin/`, `**/obj/`, `**/*.json` (секреты) — стандартный набор для .NET Docker-сборок.

---

## 4. 🔵 Suggestion

**🔵 4.1 — `DiscountRuleService`, упомянутый как эталонная реализация, в проекте отсутствует**
В кодовой базе нет файла с таким именем ни в `Services/`, ни в тестах. В качестве де-факто эталона по чистоте паттерна (Result<T>/ErrorType, ILogger, try/catch, GetByIdAsync→NotFound-check перед Update/Delete) можно ориентироваться на `CartItemService`/`ConversationService`/`OrderService` — все три построчно прочитаны и следуют единому стилю без исключений. Само по себе это не находка о проблеме, а уточнение для будущих аудитов: если `DiscountRuleService` подразумевался как часть ТЗ, его в коде физически нет.

**🔵 4.2 — Топ-продажи в аналитике группируются по строке `ProductName`, а не по `ProductId`**
Файл: `MarketTJ.Infrastructure/Persistence/Repositories/AnalyticsRepository.cs:47,106` — `GroupBy(oi => oi.ProductName)`. Поскольку `OrderItem.ProductName` — это снэпшот названия на момент заказа (осознанное решение, см. комментарий в `OrderItem.cs`), два разных объявления с одинаковым текстовым названием (например, "Помидор" от двух разных фермеров, если бы аналитика была общерыночной, а не per-farmer) задвоятся в одну строку рейтинга. Для farmer-дашборда это не проблема (там уже отфильтровано по `FarmerId`), но для общего admin-дашборда это может слегка искажать топ-продажи при совпадающих названиях у разных фермеров.
Исправление: рассмотреть группировку по `ProductListingId` с последующим маппингом в `ProductName` для отображения — не блокер, точность рейтинга.

**🔵 4.3 — `Enum.IsDefined` используется в generic-форме (`Enum.IsDefined(value)`) — это хорошо, но не 100% случаев enum-валидации покрыты**
Из 16 enum'ов явные проверки `Enum.IsDefined` найдены для: `UserRole`, `OrderStatus`, `PaymentMethod`, `PaymentStatus`, `CustomerType`, `FarmerVerificationStatus`, `ReportReason`, `ReportStatus`, `SupportTicketStatus`, `SupportPriority`, `FarmerDocumentType`, `DocumentReviewStatus`, `ListingStatus`, `DeliveryStatus`. Не найдено явной проверки для `RefundStatus` (в `RefundRequestValidator.cs:30` — фактически есть, `IsDefined(status)` найден по grep) и `StaffPermissions` ( `[Flags]`-enum, `Enum.IsDefined` для флагового enum в принципе некорректен как проверка — нужна побитовая проверка на "не выходит за пределы суммы допустимых флагов"). Не нашёл валидации `StaffPermissions` при создании/обновлении `FarmerStaffMember` — не проверено, есть ли она в `FarmerStaffMemberValidator.cs` (не прочитан построчно). Рекомендация: точечно проверить `FarmerStaffMemberValidator` на предмет валидации `Permissions` (флаговый enum корректно проверяется через `(value & ~AllFlags) == 0`, а не `Enum.IsDefined`).

**🔵 4.4 — `RegisterRequestDto`/`UserValidator` не проверяют `PhoneNumber` на формат (только "не пусто")**
Файл: `UserValidator.cs:38-39` — `PhoneNumber` проверяется только на `IsNullOrWhiteSpace`, в отличие от `Email`, у которого есть regex. Учитывая, что номер телефона используется как уникальный идентификатор для входа/восстановления в разделе ТЗ про Таджикистан (+992...), стоит добавить формат-проверку.

**🔵 4.5 — CORS-политика не ограничивает `AllowAnyHeader()`/`AllowAnyMethod()` по существу необходимого набора**
Файл: `Program.cs:61-69`. Это стандартная и в целом безопасная практика при отключённом `AllowCredentials` (что здесь так и есть — Bearer-токен, не cookie), поэтому не Warning, но для более строгого профиля безопасности можно сузить до фактически используемых методов/заголовков.

**🔵 4.6 — Health check не включает проверку Redis**
Файл: `Program.cs:99-100` — `AddHealthChecks().AddDbContextCheck<AppDbContext>()`. Кэш (`RedisCache`) используется (`CategoryRepository`, `ProductListingRepository`), но не входит в health check — при падении Redis приложение продолжит работать (репозитории просто не найдут кэш и пойдут в БД — это не баг, `GetAsync` в `RedisCacheService` не оборачивает исключения в try/catch, так что при реальной недоступности Redis запросы к `Categories`/`ProductListings` начнут падать полностью, а не деградировать до "без кэша"). Стоит добавить `AddRedis(...)` health check и/или try/catch вокруг вызовов `ICacheService` в репозиториях, чтобы недоступность Redis не роняла каталог целиком.

**🔵 4.7 — Второй `.gitignore`-слой секретов docker-compose упомянут в комментарии, но не проверялся отдельно**
Комментарий в `.gitignore` ссылается на "Секреты docker-compose (POSTGRES_PASSWORD, ANTHROPIC_API_KEY, JWT_SECRET и т.д.)" — сам `docker-compose.yml` не входил в периметр этого аудита (задание ограничено `backend/`), но с учётом находки 2.5 имеет смысл проверить его в отдельной задаче на те же риски (реальные секреты в закоммиченном или незащищённом файле).

**🔵 4.8 — `AiAssistantService` не ограничивает длину пользовательского `message` перед отправкой во внешний Anthropic API**
Файл: `AiAssistantService.cs:37`. Нет проверки на пустую строку/максимальную длину перед вызовом `SendToClaudeAsync` — не критично (это платный внешний вызов, но не эндпоинт с высоким риском), но стоит добавить базовую валидацию (аналогично `FileUploadValidator`/`ProductListingValidator` в остальном проекте).

**🔵 4.9 — `UserService.GetAllAsync`/`CreateAsync`/`UpdateAsync` проверяют уникальность email/phone через `GetAllAsync().Any(...)` вместо точечного запроса**
Файл: `UserService.cs:63-68,105-110`. Комментарий в коде сам это признаёт ("не оптимально для больших объёмов, но репозиторий не расширяю сверх того, что реально есть") — честно, но при росте базы пользователей (`GetAllAsync()` тянет всю таблицу `Users` в память на каждый Create/Update) это станет узким местом. См. также 🟡 3.6 — тот же паттерн повторяется в нескольких сервисах.

---

## 5. Что сделано хорошо

- **Единый `Result<T>`/`ErrorType` паттерн выдержан без исключений.** Все 33 сервиса и 39 контроллеров идут через `ApiControllerBase.HandleResult` — ни одного контроллера с самодельным маппингом статусов не найдено (сплошная проверка по всем файлам).
- **`GetByIdAsync` → `NotFound`-check перед `Update`/`Delete` — консистентно во всех прочитанных сервисах** (`UserService`, `OrderService`, `CartItemService`, `ConversationService`, `NotificationService` и др.) без единого исключения.
- **Try/catch + `ILogger` в каждом публичном методе сервисов**, с осмысленными русскоязычными сообщениями и правильным маппингом на `ErrorType.InternalServerError` для неожиданных исключений — стиль полностью единообразен.
- **`ExceptionHandlingMiddleware` корректно скрывает детали исключения в Production** (`environment.IsDevelopment() ? ex.Message : "Произошла внутренняя ошибка сервера"`) — никаких stack trace наружу.
- **Порядок middleware в `Program.cs` правильный**: ExceptionHandling → RequestLogging → (Swagger только в Development) → HttpsRedirection → StaticFiles → CORS → Authentication → Authorization → MapControllers — ровно тот порядок, который рекомендует Microsoft.
- **JWT-секрет вынесен из кода и из базового `appsettings.json` полностью** — обязателен через переменную окружения в Production. Это правильный подход, редко встречающийся в проектах такого масштаба без явного требования.
- **Файловая загрузка сделана аккуратно**: `LocalFileStorageService` генерирует имя файла через `Guid.NewGuid()`, не доверяя имени от клиента; `FileUploadValidator` ограничивает расширения (allow-list `.jpg/.jpeg/.png/.webp`) и размер (5 МБ); удаление старого аватара происходит только после успешной записи нового — нет риска потери файла при сбое на середине операции (`UserService.UploadAvatarAsync:277-287`).
- **Seeder полностью идемпотентен** — каждый шаг (`SeedUsersAsync`, `SeedFarmerProfileAsync`, `SeedCategoriesAsync`, `SeedProductsAsync`, `SeedProductListingsAsync`, `SeedOrdersAsync`) проверяет существование данных через `AnyAsync`/`FirstOrDefaultAsync` перед вставкой — повторный запуск на уже заполненной БД не упадёт и не задвоит данные. Пароль админа читается из `ADMIN_PASSWORD` env var с безопасным дефолтом для локальной разработки.
- **Аналитика считается на уровне БД, а не в памяти.** `AnalyticsRepository` — образцовый пример: `GroupBy`/`SumAsync`/`CountAsync`/`AverageAsync` целиком транслируются в SQL, ни одного `ToListAsync()` перед агрегацией не найдено.
- **Кэш (`ICacheService`/`RedisCacheService`) инвалидируется корректно** на каждой из операций `Add`/`Update`/`Delete` в обоих местах, где он используется (`CategoryRepository`, `ProductListingRepository`) — ни одного пропущенного `cache.RemoveAsync` не найдено.
- **DI-регистрация полная и без расхождений**: все 33 сервиса из `Services/` зарегистрированы в `AddApplicationServices`, все 30 репозиториев — в `AddInfrastructureServices`; сверка вручную не выявила ни одной сущности, зарегистрированной в интерфейсе, но забытой в DI (что вызвало бы падение только при первом реальном резолве, а не на этапе компиляции).
- **Тесты реальные и зелёные**: 806 тестов, 0 ошибок, тестовый проект покрывает все 32 сервиса из `Services/` (по одному файлу `*ServiceTests.cs` на каждый, кроме `AiAssistantService`, для которого тестового файла не найдено — см. отдельно ниже).
- **Ручные Validator-классы содержательны, а не формальны**: `ProductListingValidator` (95 строк) проверяет взаимную согласованность оптовой/розничной цены, обязательность пары "цена+мин. объём", разные правила для Create/Update (`AvailableQuantity > 0` при создании vs `>= 0` при обновлении) — это не автогенерированная заглушка, видно реальное следование бизнес-правилам ТЗ.
- **Сборка идеально чистая**: 0 предупреждений и в Debug, и в Release — редкость для проекта такого объёма (497 `.cs`-файлов), означает дисциплинированную работу с nullable reference types и нет подавленных warning'ов через `#pragma`/`NoWarn` (не проверялось построчно на `#pragma`, но `dotnet build` показал бы warning, если бы был подавлен некорректно).

**Отдельно к сведению (не находка, а факт):** тестового файла `AiAssistantServiceTests.cs` в `MarketTJ.Application.Tests/Services/` нет (список файлов — 32 файла на 33 сервиса, ровно `AiAssistantService` — пропуск). Учитывая, что сервис делает внешний HTTP-вызов к Anthropic API, тестирование потребовало бы мокать `HttpClient`/`HttpMessageHandler` — вероятно, осознанно отложено, но стоит зафиксировать как пробел в покрытии.

---

## 6. Итоговая рекомендация

**Не готово к продакшен-деплою на Railway в текущем виде.** Три находки — 2.1 (пароли без хэширования в admin-эндпоинте), 2.2 (системный IDOR на 21 контроллере) и 2.4 (нет rate limiting на login) — это не стилистические придирки, а прямые дыры в безопасности данных реальных пользователей маркетплейса (персональные данные, чужие заказы/переписки/корзины, возможность credential-stuffing). Находка 2.3 (query filter cascade) может не проявиться немедленно на маленьком датасете, но это бомба замедленного действия по мере роста soft-deleted данных — стоит закрыть в том же цикле, раз она уже подтверждена эмпирически (не просто "теоретический риск").

Рекомендуемый порядок закрытия перед деплоем:
1. 2.1 (хэширование пароля в UserService) — небольшая по объёму, но критичная правка.
2. 2.2 (IDOR) — самая объёмная работа (21 контроллер + сервисы), но именно она определяет, можно ли вообще пускать в прод многопользовательскую версию.
3. 2.4 (rate limiting на login) — типовая, быстро закрывается встроенным `Microsoft.AspNetCore.RateLimiting`.
4. 2.5 (`.dockerignore` + смена локального пароля Postgres) — быстрая правка, снижает риск случайной утечки через Docker-слои.
5. 2.3 (query filters) — требует продуманного решения по каждой из 21 связи, можно вести отдельным треком параллельно с 2.2, так как оба требуют пересмотра похожих сервисов.

После закрытия этих пяти пунктов — переоценить набор 🟡 Warning (в первую очередь 3.3 пагинация и 3.6 N+1-паттерны в сервисах) с учётом реального объёма данных, ожидаемого на первых месяцах эксплуатации.

Сборка и тесты (0/0 warnings, 806/0 tests) сами по себе не блокируют деплой — но они проверяют компиляцию и unit-логику, а не авторизацию и данные, поэтому "зелёный CI" в этом проекте не является сигналом готовности к продакшену без закрытия находок выше.
