# Техническое задание  
# Market.tj — платформа продажи сельскохозяйственных продуктов

**Версия документа:** 1.1 (адаптирована для работы с AI-агентом / Claude Code)  
**Формат проекта:** MVP  
**Команда:** 2 разработчика  
**Предполагаемый стек:** ASP.NET Core Web API (backend), React (frontend), PostgreSQL  
**Основной язык интерфейса:** таджикский  
**Дополнительный язык:** русский  

---

## 0. Инструкция для AI-агента (Claude Code)

Этот документ — единственный источник истины по проекту Market.tj. Перед началом работы прочитай его целиком, затем следуй правилам ниже.

**Порядок работы:**
1. Разработка идёт строго по этапам из раздела 23 («Порядок разработки») и спринтам из разделов 30–35. Не начинай следующий этап, пока не завершён и не проверен текущий.
2. Перед началом сессии проверь файл `PROGRESS.md` (или раздел «Текущий прогресс» в конце этого документа, если отдельного файла ещё нет) — там указано, что уже сделано.
3. После завершения этапа/спринта обнови прогресс: отметь выполненные пункты, кратко опиши, что было сделано и какие решения приняты (это должно быть достаточно, чтобы через месяц понять контекст без чтения всего кода).
4. Не добавляй функциональность, которая явно перечислена в разделе 3 как «В MVP не входят» — даже если это кажется полезным или простым.
5. Все бизнес-правила из раздела 10 обязательны к соблюдению в коде — это не рекомендации, а требования.
6. Архитектурные границы из раздела 11 и требования раздела 19 (например, «Frontend не должен напрямую обращаться к базе данных», «контроллеры не должны содержать бизнес-логику») — это hard rules, нарушать нельзя даже ради скорости. Код в Application/Infrastructure должен соблюдать принципы SOLID (раздел 11.2.2) — в частности, сервисы зависят от интерфейсов (Dependency Inversion), а не от конкретных реализаций репозиториев/кэша/файлового хранилища.
7. Формат ответа API (раздел 20) и коды ошибок (раздел 20.1, добавлено ниже) должны использоваться единообразно во всех endpoint.
8. Если что-то в задаче неоднозначно или не описано в ТЗ — не додумывай самостоятельно критичные бизнес-решения (например, связанные с деньгами, ролями, безопасностью); сначала опиши варианты и уточни у пользователя.
9. Дополнительные рекомендации (не обязательные, но желательные для качества) собраны в разделе 37.

---

## 1. Общее описание проекта

**Market.tj** — веб-платформа, которая позволяет фермерам размещать сельскохозяйственные продукты, а покупателям находить и заказывать их напрямую.

Платформа должна решить следующие проблемы:

- фермеру трудно найти постоянных покупателей;
- покупателю трудно найти свежие продукты напрямую от производителя;
- нет прозрачной информации о цене, количестве и месте производства;
- доставка между фермером и покупателем организуется вручную;
- отсутствует единая система заказов, статусов и отзывов.

Первая версия проекта должна быть небольшой, понятной и полностью рабочей.

---

## 2. Цель MVP

Создать рабочую систему, в которой:

1. фермер регистрируется;
2. администратор подтверждает фермера;
3. фермер размещает продукт;
4. покупатель находит продукт;
5. покупатель оформляет заказ;
6. фермер принимает или отклоняет заказ;
7. администратор назначает курьера;
8. курьер доставляет заказ;
9. покупатель подтверждает получение;
10. покупатель оставляет отзыв.

---

## 3. Границы первой версии

В MVP входят:

- регистрация и авторизация;
- роли пользователей;
- подтверждение фермеров;
- категории продуктов;
- товары и объявления фермеров;
- поиск и фильтрация;
- корзина;
- оформление заказа;
- изменение статусов заказа;
- ручное назначение курьера;
- доставка;
- отзывы;
- уведомления внутри системы;
- административная панель.

В MVP не входят:

- онлайн-оплата;
- настоящий escrow;
- GPS-отслеживание;
- автоматическое распределение курьеров;
- оптимизация маршрутов;
- мобильное приложение;
- SMS;
- ~~искусственный интеллект~~ — **отклонение**: добавлен AI-ассистент поиска по каталогу (Anthropic Claude API) по прямому осознанному решению пользователя, см. раздел 38;
- прогнозирование цен;
- автоматическое объединение заказов;
- продажа будущего урожая;
- сложная система споров;
- подписки и платные тарифы.

---

## 4. Роли пользователей

### 4.1. Admin

Администратор управляет всей системой.

Возможности:

- вход в административную панель;
- просмотр пользователей;
- блокировка и разблокировка пользователей;
- подтверждение или отклонение фермеров;
- управление категориями;
- просмотр объявлений;
- скрытие неподходящих объявлений;
- просмотр всех заказов;
- назначение курьера;
- просмотр доставок;
- просмотр отзывов;
- просмотр общей статистики.

---

### 4.2. Farmer

Фермер размещает и продаёт продукты.

Возможности:

- регистрация;
- вход в систему;
- заполнение профиля;
- отправка заявки на подтверждение;
- создание объявления;
- загрузка изображений;
- изменение цены;
- изменение доступного количества;
- временное скрытие объявления;
- просмотр заказов;
- принятие заказа;
- отклонение заказа;
- изменение статуса на «Готов к выдаче»;
- просмотр отзывов;
- просмотр статистики продаж.

Фермер не может публиковать объявления, пока администратор его не подтвердил.

---

### 4.3. Customer

Покупатель ищет и заказывает продукты.

Возможности:

- регистрация;
- вход;
- просмотр каталога;
- поиск продуктов;
- фильтрация;
- просмотр информации о фермере;
- добавление товара в корзину;
- изменение количества в корзине;
- оформление заказа;
- указание адреса доставки;
- просмотр истории заказов;
- отмена заказа до его принятия фермером;
- подтверждение получения;
- создание отзыва.

---

### 4.4. Courier

Курьер доставляет заказ. **Не имеет полноценной панели** — только Courier mini-interface из 2 страниц (см. раздел 14.4).

Возможности (ограниченный набор, только это):

- вход в систему;
- просмотр списка назначенных ему доставок (`My Deliveries`);
- просмотр деталей доставки (`Delivery Details`): адрес фермера, адрес покупателя, телефон фермера, телефон покупателя;
- изменение статуса: `PickedUp` → `InDelivery` → `Delivered`;
- просмотр истории доставок.

Курьер не выбирает себе доставки и не назначается сам — назначение выполняет только Admin (раздел 10.5).

---

### 4.5. Структура интерфейса: 3 части + mini-interface

Проект состоит из **3 основных частей**, а не 4 полноценных панелей:

1. **Public/Customer Website** — публичный сайт; Customer работает внутри него, отдельного dashboard у Customer нет.
2. **Farmer Panel** — полноценная панель.
3. **Admin Panel** — полноценная панель.

Плюс отдельно: **Courier mini-interface** — 2 страницы, не считается полноценной 4-й панелью (см. раздел 14.4).

---

## 5. Типы клиентов

Для покупателя рекомендуется использовать тип:

```text
Retail — розничный покупатель
Wholesale — оптовый покупатель
```

В MVP тип клиента можно сохранить, но отдельную сложную бизнес-логику для опта не реализовывать.

---

## 6. Основные пользовательские сценарии

### 6.1. Регистрация фермера

```text
Фермер открывает страницу регистрации
→ выбирает роль Farmer
→ вводит данные
→ подтверждает регистрацию
→ заполняет профиль фермера
→ отправляет профиль на проверку
→ Admin подтверждает заявку
→ фермер получает доступ к созданию объявлений
```

---

### 6.2. Создание объявления

```text
Фермер входит в систему
→ открывает «Мои объявления»
→ нажимает «Добавить»
→ выбирает продукт и категорию
→ указывает цену
→ указывает количество
→ указывает минимальный заказ
→ добавляет описание
→ добавляет изображения
→ публикует объявление
```

---

### 6.3. Оформление заказа

```text
Покупатель открывает каталог
→ выбирает продукт
→ указывает количество
→ добавляет в корзину
→ открывает корзину
→ вводит адрес
→ подтверждает заказ
→ система создаёт заказ
→ фермер получает уведомление
```

---

### 6.4. Обработка заказа

```text
Фермер получает заказ
→ проверяет количество
→ принимает или отклоняет заказ
→ готовит продукт
→ ставит статус «Готов к выдаче»
→ Admin назначает курьера
→ Courier забирает продукт
→ Courier доставляет заказ
→ Customer подтверждает получение
→ заказ завершается
```

---

## 7. Статусы

### 7.1. Статусы фермера

```csharp
public enum FarmerVerificationStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Suspended = 4
}
```

---

### 7.2. Статусы объявления

```csharp
public enum ListingStatus
{
    Draft = 1,
    Active = 2,
    Hidden = 3,
    OutOfStock = 4,
    Blocked = 5
}
```

---

### 7.3. Статусы заказа

```csharp
public enum OrderStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Preparing = 4,
    ReadyForPickup = 5,
    CourierAssigned = 6,
    PickedUp = 7,
    InDelivery = 8,
    Delivered = 9,
    Completed = 10,
    Cancelled = 11
}
```

---

### 7.4. Статусы доставки

```csharp
public enum DeliveryStatus
{
    Pending = 1,
    Assigned = 2,
    PickedUp = 3,
    InDelivery = 4,
    Delivered = 5,
    Cancelled = 6
}
```

---

## 8. Основные сущности

## 8.1. User

```text
Id
FullName
Email
PhoneNumber
PasswordHash
Role
IsActive
CreatedAt
UpdatedAt
```

Ограничения:

- Email уникальный;
- PhoneNumber уникальный;
- FullName обязательный;
- пользователь может быть заблокирован.

---

## 8.2. FarmerProfile

```text
Id
UserId
FarmName
Region
District
Village
Address
Description
VerificationStatus
VerifiedAt
VerifiedByAdminId
CreatedAt
UpdatedAt
```

Связь:

```text
User 1 — 1 FarmerProfile
```

---

## 8.3. CustomerProfile

```text
Id
UserId
CustomerType
DefaultAddress
Region
District
CreatedAt
UpdatedAt
```

---

## 8.4. CourierProfile

```text
Id
UserId
TransportType
VehicleNumber
Region
District
IsAvailable
IsActive
CreatedAt
UpdatedAt
```

Admin использует `Region`, `District`, `IsAvailable` для выбора курьера при назначении (раздел 10.5); `IsActive` — для блокировки курьера без удаления профиля.

---

## 8.5. Category

```text
Id
Name
Description
ImageUrl
IsActive
CreatedAt
UpdatedAt
```

Примеры:

- Овощи;
- Фрукты;
- Зелень;
- Сухофрукты;
- Орехи;
- Молочная продукция.

---

## 8.6. Product

Product — общий тип продукта.

```text
Id
CategoryId
Name
Description
Unit
IsActive
CreatedAt
UpdatedAt
```

Примеры:

```text
Помидор
Картофель
Лук
Яблоко
Виноград
```

---

## 8.7. ProductListing

ProductListing — конкретное предложение фермера.

```text
Id
FarmerProfileId
ProductId
Title
Description
RetailPricePerKg
WholesalePricePerKg
WholesaleMinimumQuantity
AvailableQuantity
MinimumOrderQuantity
HarvestDate
ExpectedHarvestDate
QualityGrade
Region
District
Address
Status
CreatedAt
UpdatedAt
```

Ограничения:

- цена больше 0;
- количество не меньше 0;
- минимальный заказ больше 0;
- оптовая цена не должна быть выше розничной;
- объявление принадлежит одному фермеру;
- неподтверждённый фермер не может создать активное объявление.

---

## 8.8. ProductImage

```text
Id
ProductListingId
ImageUrl
IsMain
CreatedAt
```

Ограничения:

- форматы: jpg, jpeg, png, webp;
- максимальный размер: 5 MB;
- максимум 5 изображений на объявление.

---

## 8.9. CartItem

Корзина может храниться в базе данных.

```text
Id
CustomerId
ProductListingId
Quantity
CreatedAt
UpdatedAt
```

Ограничения:

- один и тот же продукт не должен повторяться;
- количество должно быть больше 0;
- количество не должно превышать доступный остаток.

---

## 8.10. Order

```text
Id
OrderNumber
CustomerId
FarmerId
Status
DeliveryAddress
Region
District
CustomerComment
Subtotal
DeliveryPrice
TotalAmount
CreatedAt
AcceptedAt
CompletedAt
CancelledAt
```

Для MVP один заказ должен содержать товары только одного фермера.

Если в корзине товары разных фермеров, система создаёт отдельный заказ для каждого фермера.

---

## 8.11. OrderItem

```text
Id
OrderId
ProductListingId
ProductName
UnitPrice
Quantity
TotalPrice
CreatedAt
```

Важно сохранять копию:

- названия товара;
- цены;
- количества.

Изменение цены объявления не должно менять старый заказ.

Формула:

```text
TotalPrice = UnitPrice × Quantity
```

---

## 8.12. Delivery

```text
Id
OrderId
CourierId
PickupAddress
DeliveryAddress
DeliveryPrice
Status
AssignedAt
PickedUpAt
DeliveredAt
CreatedAt
UpdatedAt
```

Один заказ имеет максимум одну активную доставку.

---

## 8.13. Review

```text
Id
OrderId
CustomerId
FarmerId
Rating
Comment
CreatedAt
```

Ограничения:

- рейтинг от 1 до 5;
- отзыв создаётся только после завершения заказа;
- по одному заказу можно оставить только один отзыв;
- клиент может оставить отзыв только на свой заказ.

---

## 8.14. Notification

```text
Id
UserId
Title
Message
IsRead
CreatedAt
```

Примеры:

- новый заказ;
- заказ принят;
- заказ отклонён;
- назначен курьер;
- заказ доставлен;
- фермер подтверждён;
- фермер отклонён.

---

## 8.15. Conversation

```text
Id              int PK
OrderId         int FK → Order (unique — один чат на заказ)
CustomerId      int FK → User
FarmerId        int FK → User
IsClosed        bool
CreatedAt
UpdatedAt       (время последнего сообщения)
```

---

## 8.16. ChatMessage

```text
Id              int PK
ConversationId  int FK → Conversation
SenderId        int FK → User
Message         string
IsRead          bool
CreatedAt
```

---

## 8.17. AppSetting

```text
Id              int PK
Key             string (unique)     — например "DefaultDeliveryBasePrice", "DefaultCommissionPercent"
Value           string              — хранится строкой, парсится приложением по типу настройки
Description     string?
UpdatedAt
UpdatedByAdminId int? FK → User
```

Не хардкодить commission/базовую цену доставки в коде — читать отсюда (с кэшированием в Redis, TTL короткий, инвалидация при обновлении).

---

## 8.18. FarmerDocument

```text
Id                int PK
FarmerProfileId   int FK → FarmerProfile
DocumentType      enum(Passport, LandDeed, Other)
FileUrl           string
Status            enum(Pending, Approved, Rejected)
UploadedAt
ReviewedAt        DateTime?
ReviewedByAdminId int? FK → User
RejectionReason   string?
```

Верификация фермера (раздел 6.1) теперь требует минимум один `FarmerDocument` со статусом `Approved`, прежде чем Admin сможет подтвердить `FarmerProfile.VerificationStatus = Verified`.

---

## 8.19. AuditLog

```text
Id           int PK
AdminId      int FK → User
Action       string     — например "VerifyFarmer", "DeleteListing", "AssignCourier"
EntityType   string     — имя сущности, к которой относится действие
EntityId     int
Details      string     — JSON с деталями (было/стало)
CreatedAt
```

`EntityType`+`EntityId` — не строгий FK (полиморфная ссылка), таблица только для чтения истории Admin-действий.

---

## 8.20. ReportedListing

```text
Id                 int PK
ProductListingId   int FK → ProductListing
ReportedByUserId   int FK → User
Reason             enum(Fraud, WrongInfo, Inappropriate, Other)
Comment            string?
Status             enum(Pending, Reviewed, Dismissed)
CreatedAt
ReviewedAt         DateTime?
ReviewedByAdminId  int? FK → User
```

---

## 8.21. RefundRequest

```text
Id                 int PK
OrderId            int FK → Order
CustomerId         int FK → User
Reason             string
Amount             decimal(18,2)
Status             enum(Pending, Approved, Rejected, Refunded)
CreatedAt
ProcessedAt        DateTime?
ProcessedByAdminId int? FK → User
```

Бизнес-правило: у одного `Order` не может быть двух `RefundRequest` со статусом `Pending` одновременно.

---

## 8.22. DeliveryZone

```text
Id          int PK
Region      string
District    string
BasePrice   decimal(18,2)
PricePerKm  decimal(18,2)?
IsActive    bool
CreatedAt
UpdatedAt
```

---

## 8.23. Commission

```text
Id             int PK
CategoryId     int? FK → Category   (null = общая ставка по умолчанию)
Percentage     decimal(5,2)
EffectiveFrom  DateTime
EffectiveTo    DateTime?
CreatedAt
```

Используется при создании `Payment`/подсчёте выручки платформы: берётся ставка, у которой `EffectiveFrom <= Order.CreatedAt <= EffectiveTo` (или `EffectiveTo = null`), сначала ищется по `CategoryId`, при отсутствии — общая (`CategoryId = null`).

---

## 8.24. Payment

```text
Id                    int PK
OrderId               int FK → Order
Amount                decimal(18,2)
Method                enum(Cash, MobileMoney, Card)
Status                enum(Pending, Completed, Failed, Refunded)
PaidAt                DateTime?
TransactionReference  string?
CreatedAt
```

MVP: основной метод — `Cash` (оплата при получении), запись создаётся при `Delivered`/`Completed`. Остальные методы — задел на будущее, бизнес-логику для них сейчас не усложнять.

---

## 8.25. Favorite

```text
Id                 int PK
CustomerId         int FK → User
ProductListingId   int FK → ProductListing
CreatedAt
```

Unique index: `CustomerId` + `ProductListingId` (по сути many-to-many между User и ProductListing через эту таблицу).

---

## 8.26. FarmerStaffMember

```text
Id               int PK
FarmerProfileId  int FK → FarmerProfile
UserId           int FK → User (1:1 — один логин = один сотрудник)
Permissions      enum flags (ManageProducts, ManageStock)  — без доступа к финансам/Payment
IsActive         bool
CreatedAt
UpdatedAt
```

---

## 8.27. SupportTicket

```text
Id                 int PK
UserId             int FK → User
Subject            string
Status             enum(Open, InProgress, Closed)
Priority           enum(Low, Normal, High)
CreatedAt
ClosedAt           DateTime?
AssignedToAdminId  int? FK → User
```

Отдельно от `Conversation`/`ChatMessage` — те про конкретный заказ между Customer и Farmer, `SupportTicket` — обращение к Admin по любому вопросу.

---

## 8.28. SupportMessage

```text
Id               int PK
SupportTicketId  int FK → SupportTicket
SenderId         int FK → User
Message          string
CreatedAt
```

---

## 8.29. DeliverySlot

```text
Id        int PK
OrderId   int FK → Order (unique — один слот на заказ)
Date      DateTime (дата, без времени)
TimeFrom  string    — например "10:00"
TimeTo    string    — например "13:00"
CreatedAt
```

Клиент выбирает слот на чекауте (раздел 6.3); Admin/Courier видят его при планировании доставки.

---

## 8.30. DailySalesSnapshot

```text
Id                    int PK
Date                  DateTime (unique per день)
TotalOrders           int
TotalRevenue          decimal(18,2)
TotalCommission       decimal(18,2)
NewFarmers            int
NewCustomers          int
CompletedDeliveries   int
CreatedAt
```

Важно: это агрегат для **исторических** графиков (быстро строить чарты за прошлые дни), заполняется фоновой задачей (например раз в сутки, по данным вчерашнего дня). Текущий/сегодняшний день на Admin Dashboard считается **напрямую из живых данных** (`Order`, `Payment`) — не из snapshot, иначе сегодняшние цифры будут отставать. Snapshot не подменяет реальные данные, а ускоряет только исторические отчёты.

---

## 9. Связи сущностей

```text
User 1 — 1 FarmerProfile
User 1 — 1 CustomerProfile
User 1 — 1 CourierProfile
User 1 — 0..1 FarmerStaffMember

Category 1 — many Product
Category 1 — many Commission          (0..1 переопределение ставки на категорию)
Product 1 — many ProductListing
FarmerProfile 1 — many ProductListing
FarmerProfile 1 — many FarmerDocument
FarmerProfile 1 — many FarmerStaffMember
ProductListing 1 — many ProductImage
ProductListing 1 — many ReportedListing
ProductListing many — many User (через Favorite)

CustomerProfile 1 — many CartItem
ProductListing 1 — many CartItem

CustomerProfile 1 — many Order
FarmerProfile 1 — many Order
Order 1 — many OrderItem
ProductListing 1 — many OrderItem

Order 1 — 0..1 Delivery
Order 1 — 0..1 DeliverySlot
Order 1 — 0..many Payment
Order 1 — 0..many RefundRequest
Order 1 — 0..1 Conversation
CourierProfile 1 — many Delivery
DeliveryZone 1 — many Delivery         (через совпадение Region/District, необязательный FK)

Order 1 — 0..1 Review
FarmerProfile 1 — many Review

Conversation 1 — many ChatMessage
User 1 — many ChatMessage             (как отправитель)

User 1 — many SupportTicket
SupportTicket 1 — many SupportMessage
User 1 — many SupportMessage          (как отправитель)

User 1 — many AuditLog                (как Admin)
User 1 — many AppSetting              (как UpdatedByAdminId, необязательно)
```

---

## 10. Бизнес-правила

### 10.1. Фермер

- новый фермер получает статус Pending;
- только Admin может подтвердить фермера;
- Farmer со статусом Pending не публикует объявления;
- Farmer со статусом Suspended не принимает заказы;
- Farmer может редактировать только свои объявления;
- Farmer не может изменить завершённый заказ.

---

### 10.2. Объявление

- активное объявление отображается в каталоге;
- объявление с количеством 0 получает статус OutOfStock;
- скрытое объявление не отображается покупателю;
- Admin может заблокировать объявление;
- только владелец объявления или Admin может его изменить.

---

### 10.3. Корзина

- количество не может быть меньше минимального заказа;
- количество не может превышать остаток;
- при изменении остатка корзина должна повторно проверяться;
- товары разных фермеров создают разные заказы.

---

### 10.4. Заказ

- после создания заказ получает статус Pending;
- Customer может отменить заказ только до принятия Farmer;
- Farmer может принять или отклонить только Pending-заказ;
- количество товара резервируется при принятии заказа;
- после отклонения или отмены резерв возвращается;
- завершённый заказ нельзя редактировать;
- стоимость заказа считается на сервере;
- клиент не должен передавать итоговую стоимость вручную;
- рекомендуется idempotency-защита при создании заказа (например, idempotency key от клиента или блокировка повторной отправки формы), чтобы двойное нажатие кнопки не создавало два заказа.

---

### 10.5. Доставка

- курьера назначает только Admin; Farmer не выбирает курьера самостоятельно;
- Admin видит доступность (`IsAvailable`), регион и тип транспорта курьера перед назначением;
- нельзя назначить курьеру, у которого уже есть активная (не завершённая) доставка в этот же момент — конфликтующее назначение запрещено;
- курьер видит только назначенные ему доставки;
- курьер не может взять чужую доставку;
- Delivered устанавливается курьером;
- Completed устанавливается после подтверждения покупателем (`PATCH /api/orders/{orderId}/confirm-received`);
- в MVP оплата производится при получении.

---

### 10.6. Отзыв

- отзыв доступен только после Completed;
- отзыв нельзя создать для чужого заказа;
- рейтинг обязателен;
- комментарий необязателен.

---

## 11. Архитектура решения

> Обновлено: репозиторий — монорепозиторий с разделением backend/frontend/docs (Frontend переписан на React вместо Blazor, см. раздел 12).

```text
Market-tj/
├── backend/
│   ├── MarketTJ.Domain/
│   ├── MarketTJ.Application/
│   ├── MarketTJ.Application.Tests/
│   ├── MarketTJ.Infrastructure/
│   └── MarketTJ.WebApi/
├── frontend/
│   └── (React-приложение, см. раздел 12 и раздел 14)
└── docs/
    ├── phase-summaries/
    ├── Api.md
    ├── ArchitectureRules.md
    ├── Database.md
    ├── DesignerLogic.md
    ├── Frontend.md
    ├── MVP.md
    ├── PROGRESS.md
    ├── Roles.md
    ├── StateMachines.md
    └── Vision.md
```

---

### 11.1. MarketTJ.Domain

Содержит только:

```text
Entities/
Enums/
```

Domain не должен зависеть от других проектов.

---

### 11.2. MarketTJ.Application

```text
Application/
├── Services/
├── Interfaces/
│   ├── Repositories/       ← интерфейсы репозиториев (I...Repository)
│   └── Services/            ← интерфейсы сервисов (I...Service)
├── Common/
│   ├── PagedResult.cs
│   └── ErrorType.cs          (или Errors/ — набор типов/кодов ошибок)
├── Dto/
│   ├── AuthDto/
│   ├── FarmerDto/
│   ├── ProductDto/
│   ├── ListingDto/
│   ├── CartDto/
│   ├── OrderDto/
│   ├── DeliveryDto/
│   ├── ReviewDto/
│   └── NotificationDto/     ← у каждой сущности/фичи — своя папка DTO
└── Validators/
    ├── Auth/
    ├── Farmer/
    ├── Listing/
    ├── Order/
    └── ...                   ← валидаторы разложены по тем же группам,
                                  что и Dto; один файл на один метод/сценарий
                                  (например RegisterFarmerValidator.cs,
                                  CreateListingValidator.cs), а не один
                                  большой файл на весь модуль
```

Application зависит только от Domain.

Правило именования: если появляется новая сущность/фича — сразу создаётся своя подпапка в `Dto/` и, при необходимости, в `Validators/`. Не складывать все DTO в одну общую папку.

---

### 11.2.1. MarketTJ.Application.Tests

```text
Application.Tests/
└── Services/     ← unit-тесты бизнес-логики сервисов (по одному классу тестов
                      на сервис: AuthServiceTests, OrderServiceTests и т.д.)
```

---

### 11.2.2. Принципы SOLID (обязательно)

- **S** — один сервис = одна зона ответственности (`OrderService` не должен считать доставку и одновременно слать уведомления — для этого свой `INotificationService`);
- **O** — новую бизнес-логику добавлять новыми классами/методами, не переписывая рабочие куски существующих сервисов "на живую";
- **L** — любая реализация интерфейса (`IOrderRepository`, `ICacheService`, `IFileStorageService`) должна быть взаимозаменяема без изменения кода, который её вызывает;
- **I** — интерфейсы узкие и по делу (`IOrderRepository`, а не один гигантский `IRepository` на все сущности сразу с десятками несвязанных методов);
- **D** — сервисы в Application зависят только от интерфейсов из `Application/Interfaces/` (Repositories, Services), реализации в Infrastructure подставляются через DI в `Program.cs`. Application не должен содержать `using` на EF Core, Npgsql, StackExchange.Redis или любые Infrastructure-пакеты напрямую.

---

### 11.3. MarketTJ.Infrastructure

```text
Infrastructure/
└── Persistence/
    ├── AppDbContext.cs
    ├── Repositories/         ← реализации I...Repository из Application
    ├── Configurations/       ← EntityTypeConfiguration для каждой сущности
    └── Seeder/               ← начальные данные (роли, admin, категории — раздел 22)
```

Infrastructure зависит от Domain и Application. Миграции (`Migrations/`) генерируются EF Core внутри `Persistence/` стандартным образом.

Файловое хранилище (`IFileStorageService`, см. раздел 17) и уведомления также размещаются в Infrastructure — при необходимости заведи под них отдельные папки рядом с `Persistence/` (например `FileStorage/`, `Notifications/`), не смешивая их с Persistence. Redis-кэш (`RedisCacheService` — реализация `ICacheService`, раздел 18) размещается аналогично, в отдельной папке `Caching/`, не внутри `Persistence/`.

---

### 11.4. MarketTJ.WebApi

```text
WebApi/
└── Controllers/     ← все контроллеры здесь
```

Middleware, фильтры, авторизация и Program.cs остаются на уровне проекта WebApi (вне Controllers/), контроллеры не должны содержать бизнес-логику — только вызов сервисов из Application.

---

### 11.5. frontend (React)

```text
frontend/
├── src/
│   ├── pages/
│   │   ├── public/       ← Public/Customer Website (раздел 14.1)
│   │   ├── farmer/       ← Farmer Panel (раздел 14.2)
│   │   ├── admin/        ← Admin Panel (раздел 14.3)
│   │   └── courier/      ← Courier mini-interface, 2 страницы (раздел 14.4)
│   ├── components/
│   ├── layouts/          ← отдельный layout на каждую из 3 частей;
│   │                        у courier/ своего layout с меню нет (раздел 4.5)
│   ├── api/            ← клиенты для вызова backend API
│   ├── auth/            ← хранение токена, защищённые роуты
│   └── state/           ← глобальное состояние (например корзина)
└── public/
```

Frontend взаимодействует с сервером только через Web API (HTTP), без прямого доступа к базе данных.

Правильная схема:

```text
React
→ HTTP API
→ Application
→ Infrastructure
→ PostgreSQL
```

---

## 12. Технологии

### Backend

- .NET 10;
- ASP.NET Core Web API;
- Entity Framework Core;
- PostgreSQL;
- Redis (StackExchange.Redis) — кэширование каталога/справочников;
- ASP.NET Core Identity;
- JWT Bearer;
- FluentValidation;
- AutoMapper или ручной mapping;
- Swagger;
- Serilog.

### Frontend

- **React** (вместо ранее предполагавшегося Blazor);
- TypeScript (рекомендуется для крупного проекта с 2 разработчиками);
- React Router — для маршрутизации между страницами (раздел 14);
- Axios или fetch — HTTP-клиент к Web API;
- Управление состоянием: React Context (для auth/корзины на MVP достаточно, Redux/Zustand не обязателен);
- Стилизация: любая (Tailwind CSS / CSS Modules / Bootstrap) — выбор Frontend-разработчика, если не оговорено отдельно;
- хранение токена: localStorage или защищённое хранилище — не в plain cookie без HttpOnly;
- адаптивный дизайн.

> Если фактический выбор (TypeScript vs JS, конкретная UI-библиотека, стейт-менеджер) будет отличаться от указанного — обнови этот раздел и `docs/Frontend.md`, чтобы он не расходился с кодом.

### Тестирование

- xUnit;
- FluentAssertions;
- интеграционные тесты для основных API;
- на Frontend — тесты не обязательны для MVP, но приветствуются (например Vitest/React Testing Library) для критичной логики (корзина, чекаут).

---

## 13. API endpoints

## 13.1. Auth

```http
POST /api/auth/register/customer
POST /api/auth/register/farmer
POST /api/auth/login
GET  /api/auth/me
POST /api/auth/change-password
```

---

## 13.2. Farmers

```http
GET    /api/farmers
GET    /api/farmers/{id}
GET    /api/farmers/me
PUT    /api/farmers/me
GET    /api/admin/farmers/pending
PATCH  /api/admin/farmers/{id}/approve
PATCH  /api/admin/farmers/{id}/reject
PATCH  /api/admin/farmers/{id}/suspend
```

---

## 13.3. Categories

```http
GET    /api/categories
GET    /api/categories/{id}
POST   /api/categories
PUT    /api/categories/{id}
DELETE /api/categories/{id}
```

Create, Update ва Delete танҳо барои Admin.

---

## 13.4. Products

```http
GET    /api/products
GET    /api/products/{id}
POST   /api/products
PUT    /api/products/{id}
DELETE /api/products/{id}
```

Идоракунӣ танҳо барои Admin.

---

## 13.5. Product Listings

```http
GET    /api/listings
GET    /api/listings/{id}
GET    /api/listings/my
POST   /api/listings
PUT    /api/listings/{id}
DELETE /api/listings/{id}
PATCH  /api/listings/{id}/hide
PATCH  /api/listings/{id}/activate
POST   /api/listings/{id}/images
DELETE /api/listings/{id}/images/{imageId}
```

Филтрҳо:

```text
search
categoryId
farmerId
region
district
minPrice
maxPrice
availableOnly
sortBy
pageNumber
pageSize
```

---

## 13.6. Cart

```http
GET    /api/cart
POST   /api/cart/items
PUT    /api/cart/items/{id}
DELETE /api/cart/items/{id}
DELETE /api/cart/clear
```

---

## 13.7. Orders

```http
POST   /api/orders
GET    /api/orders/my
GET    /api/orders/{id}
PATCH  /api/orders/{id}/cancel
PATCH  /api/orders/{id}/confirm-received

GET    /api/farmer/orders
PATCH  /api/farmer/orders/{id}/accept
PATCH  /api/farmer/orders/{id}/reject
PATCH  /api/farmer/orders/{id}/preparing
PATCH  /api/farmer/orders/{id}/ready-for-pickup

GET    /api/admin/orders
GET    /api/admin/orders/ready-for-pickup
```

---

## 13.8. Deliveries

```http
GET    /api/admin/couriers/available
POST   /api/admin/deliveries/assign

GET    /api/courier/deliveries/my
GET    /api/courier/deliveries/{id}
PATCH  /api/courier/deliveries/{id}/pickup
PATCH  /api/courier/deliveries/{id}/start
PATCH  /api/courier/deliveries/{id}/deliver
```

`GET /api/admin/couriers/available` возвращает курьеров с `IsAvailable=true`, `IsActive=true`, с учётом региона/типа транспорта (раздел 10.5).

---

## 13.9. Reviews

```http
POST   /api/reviews
GET    /api/farmers/{farmerId}/reviews
GET    /api/reviews/my
DELETE /api/reviews/{id}
```

Удаление доступно владельцу или Admin.

---

## 13.10. Notifications

```http
GET    /api/notifications
PATCH  /api/notifications/{id}/read
PATCH  /api/notifications/read-all
```

---

## 13.11. Chat (Conversation / ChatMessage)

```http
GET    /api/orders/{orderId}/conversation
GET    /api/conversations/{id}/messages
POST   /api/conversations/{id}/messages
```

Доступ только у `CustomerId`/`FarmerId` конкретного `Order` (раздел 8.15).

---

## 13.12. Favorites

```http
GET    /api/favorites
POST   /api/favorites/{listingId}
DELETE /api/favorites/{listingId}
```

---

## 13.13. Support

```http
POST   /api/support/tickets
GET    /api/support/tickets/my
GET    /api/admin/support/tickets
GET    /api/support/tickets/{id}/messages
POST   /api/support/tickets/{id}/messages
PATCH  /api/admin/support/tickets/{id}/close
```

---

## 13.14. Farmer documents (верификация)

```http
POST   /api/farmer/documents
GET    /api/farmer/documents/my
GET    /api/admin/farmers/{farmerId}/documents
PATCH  /api/admin/documents/{id}/approve
PATCH  /api/admin/documents/{id}/reject
```

---

## 13.15. Reports & Refunds

```http
POST   /api/listings/{id}/report
GET    /api/admin/reported-listings
PATCH  /api/admin/reported-listings/{id}/review

POST   /api/orders/{id}/refund-request
GET    /api/admin/refund-requests
PATCH  /api/admin/refund-requests/{id}/approve
PATCH  /api/admin/refund-requests/{id}/reject
```

---

## 13.16. Admin settings & lookups

```http
GET    /api/admin/settings
PUT    /api/admin/settings/{key}

GET    /api/admin/delivery-zones
POST   /api/admin/delivery-zones
PUT    /api/admin/delivery-zones/{id}

GET    /api/admin/commissions
POST   /api/admin/commissions
```

---

## 13.17. Farmer staff

```http
GET    /api/farmer/staff
POST   /api/farmer/staff
PATCH  /api/farmer/staff/{id}
DELETE /api/farmer/staff/{id}
```

Доступно только владельцу `FarmerProfile` (не самому сотруднику).

---

## 13.18. Delivery slots & payments

```http
POST   /api/orders/{id}/delivery-slot     (при checkout, раздел 8.29)

GET    /api/admin/payments
POST   /api/admin/payments               (ручная фиксация оплаты при получении)
```

---

## 13.19. Analytics

```http
GET    /api/admin/analytics/dashboard?from=&to=
GET    /api/admin/analytics/history?from=&to=
```

- `dashboard` — данные **за сегодня/выбранный текущий период** считаются напрямую по `Order`/`Payment` (живые данные, раздел 8.30);
- `history` — данные за прошлые дни отдаются из `DailySalesSnapshot` (быстрее, без пересчёта старых заказов).

---

## 14. Страницы (React, маршруты)

> Обновлено: 4 полноценные панели заменены на 3 части + Courier mini-interface (раздел 4.5). Customer не имеет отдельного dashboard — работает внутри Public/Customer Website.

## 14.1. Public/Customer Website

```text
/
/catalog
/product/:id
/cart
/checkout
/orders
/orders/:id
/orders/:id/chat
/favorites
/support
/profile
/login
/register
/about
/contact
/forbidden
/not-found
```

Здесь же живёт Customer — отдельного `/customer/dashboard` нет.

---

## 14.2. Farmer Panel

```text
/farmer
/farmer/products
/farmer/products/create
/farmer/products/:id/edit
/farmer/orders
/farmer/orders/:id
/farmer/orders/:id/chat
/farmer/reviews
/farmer/profile
/farmer/documents
/farmer/staff
/farmer/notifications
```

`/farmer` — Dashboard фермера (главная страница панели). `/farmer/products/*` управляют сущностью `ProductListing` (раздел 8.7) — в UI используется слово "продукт/товар", в коде/БД сущность остаётся `ProductListing`, переименовывать её не нужно.

Страница верификации (`Farmer verification`, раздел 6.1) не удаляется — она встроена в `/farmer/profile` (статус верификации и форма отправки на проверку показываются там же, отдельная страница не нужна).

---

## 14.3. Admin Panel

```text
/admin
/admin/users
/admin/farmers
/admin/farmers/pending
/admin/categories
/admin/products
/admin/orders
/admin/couriers
/admin/deliveries
/admin/reviews
/admin/statistics
/admin/reported-listings
/admin/refund-requests
/admin/support-tickets
/admin/settings
/admin/delivery-zones
/admin/commissions
```

`/admin` — Dashboard. `/admin/couriers` — список курьеров с `IsAvailable`/регионом (для назначения, раздел 13.8).

---

## 14.4. Courier mini-interface

```text
/courier/deliveries
/courier/deliveries/:id
```

Это **не** полноценная 4-я панель — только 2 страницы (`My Deliveries`, `Delivery Details`), без Dashboard, без отдельного layout с боковым меню (см. раздел 4.5, 4.4).

---

## 15. Требования к интерфейсу

Интерфейс должен быть:

- адаптивным;
- удобным для телефона;
- простым;
- без перегруженных форм;
- с крупными кнопками;
- с понятными статусами;
- с таджикским языком;
- с возможностью добавить русский язык;
- с подтверждением опасных действий;
- с сообщениями об ошибках;
- с индикаторами загрузки.

Для фермера особенно важно:

- минимум текста;
- короткие формы;
- понятные иконки;
- возможность загрузить фото с телефона;
- быстрое изменение цены и количества.

---

## 16. Безопасность

Обязательные требования:

- пароли хранить через Identity;
- JWT должен содержать UserId и Role;
- endpoints защищать через `[Authorize]`;
- операции ролей защищать через `[Authorize(Roles = "...")]`;
- проверять владельца сущности;
- не принимать UserId из клиента, если его можно получить из токена;
- валидировать DTO;
- ограничивать размер файлов;
- проверять расширение файла;
- использовать HTTPS;
- не возвращать PasswordHash;
- не хранить секреты в Git;
- использовать глобальный exception middleware;
- логировать критические ошибки;
- использовать pagination;
- защищаться от отрицательных цен и количества.

Дополнительно (рекомендуется для MVP, не блокирует Definition of Done):

- использовать короткоживущий access token (JWT) + refresh token, а не только долгоживущий JWT;
- добавить rate limiting на `/api/auth/login` и `/api/auth/register/*` (защита от brute-force и спама регистраций).

---

## 17. Работа с файлами

Файлы хранятся:

```text
wwwroot/uploads/listings/{listingId}/
```

В базе хранится только путь:

```text
/uploads/listings/15/image-1.jpg
```

Ограничения:

```text
Разрешённые форматы: jpg, jpeg, png, webp
Максимальный размер: 5 MB
Максимум изображений: 5
```

При удалении объявления связанные изображения должны удаляться или помечаться удалёнными.

Рекомендация: реализовать доступ к файлам через абстракцию `IFileStorageService` (а не напрямую через `File.Save`), чтобы в будущем можно было перейти на облачное хранилище (S3-совместимое) без переписывания бизнес-логики.

---

## 18. Общие требования к данным

Для денежных значений:

```csharp
decimal
```

Настройка:

```csharp
HasPrecision(18, 2)
```

Для количества:

```csharp
HasPrecision(18, 3)
```

Общие поля сущностей:

```text
CreatedAt
UpdatedAt
IsDeleted
DeletedAt
```

Для важных данных рекомендуется soft delete.

Идентификаторы (`Id`):

```csharp
public int Id { get; set; }   // не Guid — обычный auto-increment int для всех сущностей
```

Все внешние ключи (`UserId`, `CourierId`, `OrderId` и т.д.) — тоже `int`.

Кэширование (Redis):

- используется для данных, которые часто читаются и редко меняются: каталог (`Category`, публичный список `ProductListing`), справочники (`RefusalReason`-подобные lookup, если появятся);
- ключ кэша строится по шаблону `market:{entity}:{id}` или `market:{entity}:list:{queryHash}`;
- инвалидация — при любом Create/Update/Delete соответствующей сущности кэш этого ключа удаляется явно (не полагаться только на TTL);
- TTL по умолчанию: 5–10 минут для списков каталога;
- доступ к кэшу — через интерфейс `ICacheService` в `Application/Interfaces/Services/`, реализация `RedisCacheService` в `Infrastructure` (аналогично `IFileStorageService`, раздел 17) — сервисы Application не должны знать о `StackExchange.Redis` напрямую.

---

## 19. Нефункциональные требования

- время ответа API для обычных запросов — до 2 секунд;
- каталог должен использовать pagination;
- приложение должно корректно работать на телефоне;
- ошибки должны иметь единый формат;
- Swagger должен открываться без ошибок;
- база данных должна создаваться через migrations;
- проект должен запускаться после настройки connection string;
- код должен быть разделён по слоям;
- контроллеры не должны содержать бизнес-логику;
- Frontend (React) не должен напрямую обращаться к базе данных — только через Web API.

---

## 20. Формат ответа API

Успешный ответ:

```json
{
  "isSuccess": true,
  "message": "Operation completed successfully",
  "data": {}
}
```

Ответ с ошибкой:

```json
{
  "isSuccess": false,
  "message": "Validation failed",
  "errors": [
    "Price must be greater than zero"
  ]
}
```

Для pagination:

```json
{
  "items": [],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 100,
  "totalPages": 5
}
```

### 20.1. Коды ошибок (рекомендация)

Так как интерфейс двуязычный (таджикский/русский), сообщения об ошибках лучше не хардкодить строкой на сервере, а возвращать машинно-читаемый код, который Frontend сам переводит на нужный язык:

```json
{
  "isSuccess": false,
  "message": "Validation failed",
  "errorCode": "PRICE_MUST_BE_POSITIVE",
  "errors": [
    "Price must be greater than zero"
  ]
}
```

`message`/`errors` можно оставить как есть (для логов и Swagger), но `errorCode` — стабильный ключ, который не должен меняться и на который завязывается локализация на клиенте.

---

## 21. Валидация

Примеры:

### Register

```text
FullName: обязательный, 3–100 символов
Email: обязательный, корректный
PhoneNumber: обязательный
Password: минимум 6 символов
```

### ProductListing

```text
Title: обязательный, 3–150 символов
Description: максимум 2000 символов
RetailPricePerKg: больше 0
WholesalePricePerKg: больше 0 или null
AvailableQuantity: больше 0
MinimumOrderQuantity: больше 0
```

### Order

```text
DeliveryAddress: обязательный
Cart: не пустой
Quantity: доступно на складе
Customer: активный
Farmer: подтверждённый
```

---

## 22. Начальные данные

При первом запуске создаются:

### Роли

```text
Admin
Farmer
Customer
Courier
```

### Admin

```text
Email: admin@market.tj
Password: задаётся через environment variable
```

### Категории

```text
Овощи
Фрукты
Зелень
Сухофрукты
Орехи
Молочная продукция
```

---

## 23. Порядок разработки

### Этап 1. Основа проекта

- создать solution;
- создать проекты по слоям;
- настроить references;
- подключить PostgreSQL;
- создать AppDbContext;
- создать базовые entity;
- настроить migrations;
- настроить Result pattern;
- настроить exception middleware;
- настроить Swagger.

Результат:

```text
Проект запускается
База создаётся
Swagger работает
```

---

### Этап 2. Authentication

- User;
- Identity;
- роли;
- регистрация Customer;
- регистрация Farmer;
- login;
- JWT;
- AdminSeeder;
- CurrentUserService.

Результат:

```text
Пользователь регистрируется
Пользователь входит
Role authorization работает
```

---

### Этап 3. Farmer verification

- FarmerProfile;
- заполнение профиля;
- список Pending;
- approve;
- reject;
- suspend.

Результат:

```text
Admin подтверждает фермера
Подтверждённый фермер создаёт объявления
```

---

### Этап 4. Каталог

- Category;
- Product;
- ProductListing;
- ProductImage;
- CRUD;
- поиск;
- фильтры;
- pagination.

Результат:

```text
Фермер создаёт объявление
Покупатель видит каталог
```

---

### Этап 5. Корзина и заказ

- CartItem;
- Order;
- OrderItem;
- расчёт суммы;
- создание заказов;
- разделение по фермерам;
- история заказов.

Результат:

```text
Покупатель оформляет заказ
Фермер видит заказ
```

---

### Этап 6. Обработка заказа

- accept;
- reject;
- preparing;
- ready;
- cancel;
- резервирование количества;
- возврат остатка при отмене.

Результат:

```text
Статусы заказа работают
Остатки обновляются корректно
```

---

### Этап 7. Доставка

- CourierProfile;
- создание курьера;
- назначение курьера;
- статусы доставки;
- подтверждение получения.

Результат:

```text
Курьер получает заказ
Заказ завершается
```

---

### Этап 8. Отзывы и уведомления

- Review;
- Notification;
- рейтинг фермера;
- внутренние уведомления.

Результат:

```text
Покупатель оставляет отзыв
Пользователи получают уведомления
```

---

### Этап 9. Тестирование и улучшение

- unit-тесты;
- integration-тесты;
- проверка ролей;
- проверка ownership;
- проверка адаптивности;
- исправление ошибок;
- подготовка README;
- demo data.

---

## 24. Разделение работы между двумя разработчиками

## Разработчик 1 — Backend

Основные задачи:

- Domain;
- Application;
- Infrastructure;
- AppDbContext;
- configurations;
- migrations;
- repositories;
- services;
- Web API;
- authentication;
- authorization;
- Swagger;
- validation;
- tests.

Дополнительные задачи:

- подготовка DTO;
- API documentation;
- seed данных;
- code review frontend API-клиентов.

---

## Разработчик 2 — Frontend

Основные задачи:

- React (проект, роутинг, структура папок из раздела 11.5);
- layouts (Public/Customer Website, Farmer Panel, Admin Panel — 3 отдельных layout, у Courier mini-interface своего layout с меню нет, раздел 4.5);
- authentication UI;
- service clients;
- Public/Customer Website (страницы раздела 14.1);
- Farmer Panel (раздел 14.2);
- Admin Panel (раздел 14.3);
- Courier mini-interface (2 страницы, раздел 14.4);
- forms;
- validation messages;
- loading;
- responsive design.

Дополнительные задачи:

- локализация;
- UI-компоненты;
- ручное тестирование;
- demo preparation;
- code review API DTO.

---

## Совместные задачи

Оба разработчика должны вместе согласовать:

- DTO;
- endpoint names;
- enum values;
- API response format;
- роли;
- status transitions;
- Git branches;
- merge rules;
- naming conventions.

---

## 25. Git workflow

Рекомендуемые ветки:

```text
main
develop
feature/auth
feature/farmer-verification
feature/listings
feature/cart
feature/orders
feature/delivery
feature/reviews
```

Правила:

- работа ведётся в feature-ветке;
- изменения объединяются через pull request;
- перед merge второй разработчик проверяет код;
- main содержит только стабильную версию;
- большие изменения не делать напрямую в main;
- commit должен описывать одно логическое изменение.

Примеры commit:

```text
feat: add farmer verification
fix: prevent ordering unavailable quantity
refactor: move order logic to service
docs: update API endpoints
```

---

## 26. Definition of Done

Функция считается завершённой, если:

- код компилируется;
- endpoint работает в Swagger;
- UI работает;
- validation добавлена;
- role authorization работает;
- ошибки обрабатываются;
- база обновлена migration;
- нет критических warning;
- выполнено ручное тестирование;
- код проверен вторым разработчиком.

---

## 27. Критерии приёмки MVP

MVP считается готовым, если выполняются все пункты:

### Authentication

- Customer регистрируется;
- Farmer регистрируется;
- пользователь входит;
- JWT работает;
- роли защищают endpoints.

### Farmer

- Farmer заполняет профиль;
- Admin подтверждает Farmer;
- неподтверждённый Farmer не создаёт объявление;
- подтверждённый Farmer создаёт и редактирует объявление.

### Catalog

- Customer видит активные объявления;
- работает поиск;
- работают фильтры;
- работает pagination;
- отображаются изображения, цена и остаток.

### Cart

- Customer добавляет товар;
- Customer изменяет количество;
- система проверяет остаток;
- система считает сумму.

### Order

- Customer создаёт заказ;
- товары разных фермеров разделяются;
- Farmer принимает или отклоняет заказ;
- Customer видит историю;
- Customer может отменить Pending-заказ.

### Delivery

- Admin назначает Courier;
- Courier видит назначенную доставку;
- Courier изменяет статусы;
- Customer подтверждает получение;
- заказ получает Completed.

### Review

- Customer оставляет отзыв;
- отзыв доступен только после Completed;
- рейтинг фермера пересчитывается.

### Admin

- Admin видит пользователей;
- Admin подтверждает фермеров;
- Admin управляет категориями;
- Admin видит заказы;
- Admin назначает курьера;
- Admin блокирует пользователя.

---

## 28. Возможные улучшения после MVP

### Версия 2

- онлайн-оплата;
- escrow;
- dispute;
- доказательства фото и видео;
- продажа будущего урожая;
- оптовые заявки;
- автоматическое назначение курьера;
- точки самовывоза;
- SMS;
- Telegram-уведомления;
- карта;
- GPS;
- объединение заказов по району;
- скидки;
- промокоды.

### Версия 3

- мобильное приложение;
- голосовой ввод на таджикском;
- AI-рекомендации;
- прогноз спроса;
- прогноз цен;
- динамическая цена;
- подписка на еженедельную корзину;
- кооперация фермеров;
- аналитика по регионам;
- экспорт отчётов.

---

## 29. Риски проекта

### Большой объём

Решение:

- не добавлять функции вне MVP;
- завершать один модуль до начала другого.

### Сложность заказов

Решение:

- один заказ — один фермер;
- стоимость рассчитывается только сервером;
- использовать transaction.

### Несовпадение Backend и Frontend

Решение:

- заранее согласовать DTO;
- использовать Swagger;
- не менять API без согласования.

### Ошибки ролей

Решение:

- проверять claims;
- использовать единые role constants;
- тестировать каждый endpoint с разными ролями.

### Потеря количества товара

Решение:

- проверять остаток перед принятием;
- использовать transaction;
- добавить concurrency token.

---

## 30. Рекомендуемый первый Sprint

Продолжительность: 1 неделя.

### Backend

```text
Создать solution
Настроить архитектуру
Подключить PostgreSQL
Создать User
Настроить Identity
Создать роли
Создать Register
Создать Login
Создать JWT
Создать AdminSeeder
```

### Frontend

```text
Создать React project (структура src/pages, src/components, src/layouts, src/api, src/auth, src/state)
Создать Layout
Создать Login page
Создать Register page
Создать AuthApiClient (src/api)
Создать TokenStorage (src/auth)
Создать AuthContext / защищённые роуты
Создать RedirectToLogin
```

### Итог Sprint

```text
Customer регистрируется
Farmer регистрируется
Пользователь входит
JWT сохраняется
Role отображается
Защищённая страница открывается
Logout работает
```

---

## 31. Рекомендуемый второй Sprint

### Backend

```text
FarmerProfile
Farmer verification
Category CRUD
Product CRUD
```

### Frontend

```text
Farmer profile page
Admin pending farmers page
Admin categories page
Admin products page
```

### Итог Sprint

```text
Farmer отправляет профиль
Admin подтверждает его
Admin управляет категориями
Admin управляет продуктами
```

---

## 32. Рекомендуемый третий Sprint

### Backend

```text
ProductListing
ProductImage
Search
Filters
Pagination
```

### Frontend

```text
Farmer listings
Create listing
Edit listing
Catalog
Listing details
```

### Итог Sprint

```text
Farmer публикует продукт
Customer видит и находит его
```

---

## 33. Рекомендуемый четвёртый Sprint

### Backend

```text
Cart
Order
OrderItem
Accept
Reject
Cancel
Stock reservation
```

### Frontend

```text
Cart page
Checkout page
Customer orders
Farmer orders
Order details
```

### Итог Sprint

```text
Покупатель оформляет заказ
Фермер обрабатывает заказ
```

---

## 34. Рекомендуемый пятый Sprint

### Backend

```text
CourierProfile
Delivery
Assign courier
Delivery statuses
Confirm received
```

### Frontend

```text
Admin delivery assignment
Courier deliveries
Delivery details
Customer confirmation
```

### Итог Sprint

```text
Заказ проходит полный путь до завершения
```

---

## 35. Рекомендуемый шестой Sprint

### Backend

```text
Review
Notification
Dashboard statistics
Tests
Bug fixing
```

### Frontend

```text
Review form
Notification page
Dashboards
Responsive fixes
Final testing
```

### Итог Sprint

```text
MVP готов к демонстрации
```

---

## 36. Финальный результат

После выполнения ТЗ должна существовать система, в которой:

```text
Farmer
→ проходит проверку
→ публикует продукт
→ получает заказ
→ передаёт заказ курьеру

Customer
→ находит продукт
→ оформляет заказ
→ получает доставку
→ оставляет отзыв

Admin
→ управляет платформой
→ подтверждает фермеров
→ назначает курьера

Courier
→ получает доставку
→ меняет статусы
→ завершает доставку
```

Главный принцип проекта:

> Сначала полностью рабочий MVP, затем дополнительные функции.

---

## 37. Дополнительные рекомендации (сводка)

Эти пункты не входят в обязательный Definition of Done, но повышают качество и снижают риски на будущее. Рекомендуется учитывать их сразу при написании кода, чтобы не переделывать позже:

1. **Refresh token** — access token (JWT) должен быть короткоживущим, плюс отдельный refresh token (см. раздел 16).
2. **Rate limiting** на auth-endpoints — защита от brute-force (см. раздел 16).
3. **Абстракция файлового хранилища** (`IFileStorageService`) — вместо прямой работы с диском, для лёгкого перехода на облако позже (см. раздел 17).
4. **Машинно-читаемые коды ошибок** (`errorCode`) — вместо жёстко закодированных строк, для корректной локализации на двух языках (см. раздел 20.1).
5. **Idempotency при создании заказа** — защита от двойного нажатия/повторной отправки (см. раздел 10.4).

Если что-то из этого будет реализовано, обнови этот раздел (отметь пункт как выполненный) и добавь короткую запись в соответствующий phase-summary.

---

## 37.1. Разбивка на сессии/этапы и шаги (Phase → Step)

Разработка ведётся не просто по этапам из раздела 23, а по более мелким **шагам** внутри каждого этапа — так AI-агенту (и разработчикам) проще работать сессия за сессией, не теряя контекст.

Правила:

1. Каждый этап (Phase0, Phase1, Phase2, Phase3, Phase3.5, Phase4...) состоит из пронумерованных шагов (Step 1, Step 2...).
2. Один шаг = одна законченная, проверяемая единица работы (например «Step 1: Repository scaffold», «Step 7: date range filter — presets + custom period»).
3. Каждый шаг помечается тегом, к какой части относится:
   - `[BE]` — только backend;
   - `[FE]` — только frontend;
   - `[FULL]` — затрагивает и backend, и frontend.
4. Commit-сообщение оформляется по шаблону:
   ```
   Phase{N} Step {M} [BE|FE|FULL]: краткое описание сделанного
   ```
   Примеры:
   ```
   Phase 0 Step 1 [FULL]: Repository scaffold
   Phase 3.5 Step 9 [FULL]: replace fixed configurations with panels array
   Phase 4 Step 7 [FE]: date range filter — presets + custom period
   ```
5. После завершения этапа целиком — итог фиксируется в `docs/phase-summaries/Phase{N}-summary.md` (что сделано по шагам, какие решения приняты, какие проблемы возникли).
6. Если AI-агент не уверен, к какому шагу/этапу относится текущая задача — сначала уточняет у пользователя, а не придумывает произвольную нумерацию.

Это не блокирует Definition of Done, но обязательно для истории коммитов и для того, чтобы Claude Code мог быстро восстановить контекст в новой сессии, прочитав только `docs/PROGRESS.md` и последний `phase-summary`, не читая весь код.

---

## 37.2. Шаблон `phase-summaries/PhaseN-summary.md`

Каждый summary обязателен к заполнению по этому скелету (пустые секции недопустимы — если пункта нет, писать "—"):

```markdown
# Phase N — <Название> Summary

**Status:** ✅ Complete / 🚧 In Progress
**Completed:** YYYY-MM-DD
**Branch:** ...
**Tag:** vX.Y-название

---

## What Was Built

### Backend
| Item | Description |
|------|-------------|
| ... | ... |

### Frontend
| Item | Description |
|------|-------------|

---

## Key Files
(backend/... и frontend/... — реальные пути к главным новым/изменённым файлам)

---

## Migrations Applied
| Migration | Description |
|-----------|-------------|

---

## Architecture Decisions
| Decision | Reasoning |
|----------|-----------|

---

## Known Issues / Tech Debt
| Issue | Target Phase |
|-------|-------------|

---

## Test Coverage
| Suite | Tests | What's Covered |
|-------|-------|----------------|

---

## Next Phase Overview
(2-4 предложения о том, что и почему делает следующий этап, и какая часть текущего этапа является для него основой)
```

Таблица «Architecture Decisions» обязательна даже для маленьких этапов — именно она экономит больше всего времени AI-агенту в новой сессии.

---

## 37.3. Формат `docs/Api.md`

`docs/Api.md` — не просто список endpoint (раздел 13), а документ конвенций:

```markdown
# API — REST Conventions

Base URL: /api/v1
Все endpoint требуют JWT, кроме /api/v1/auth/login и /api/v1/auth/register/*

## Conventions
- Versioning через URL
- JSON only, camelCase
- DateTimes: ISO 8601 UTC
- Money (RetailPricePerKg и т.п.): decimal, 2 знака
- IDs: int (auto-increment), не GUID
- Pagination: ?pageNumber=&pageSize= → { items, pageNumber, pageSize, totalCount, totalPages } (раздел 20)
- Sorting/Filtering: раздел 13.5 (search, categoryId, region и т.д.)

## Ответы (раздел 20 + 20.1)
[оставить формат isSuccess/message/data/errorCode как уже определено в ТЗ —
не заменять на ProblemDetails/RFC 7807, если явно не согласовано отдельно]

| HTTP | Когда |
|------|-------|
| 200 | OK |
| 201 | Created |
| 204 | No content |
| 400 | Validation failed |
| 401 | Нет/неверный JWT |
| 403 | Роль не разрешена |
| 404 | Не найдено / чужая сущность |
| 409 | Конфликт статуса |
| 422 | Нарушено бизнес-правило |
| 500 | Server error |

## Endpoint catalogue
[endpoint-ы из раздела 13, сгруппированные по ресурсам: Auth, Farmers, Categories,
Products, Listings, Cart, Orders, Deliveries, Reviews, Notifications —
в том же табличном/списочном виде, как в разделе 13]
```

---

## 37.4. Автоматический лог `backend/progress/` (без напоминаний)

В отличие от `docs/PROGRESS.md` (общий чек-лист, раздел 26–27) и `docs/phase-summaries/` (итог по завершении целого Phase, раздел 37.2) — это третий, самый мелкий уровень: лог **каждого** законченного действия, ведётся автоматически, без просьбы пользователя.

Правило для AI-агента (закреплено также в `CLAUDE.md` в корне репозитория, см. ниже):

1. После завершения любого законченного действия (реализована сущность, готов endpoint, исправлен баг, применена миграция и т.д.) — сразу открой/создай файл `backend/progress/YYYY-MM-DD.md` (дата — сегодняшняя).
2. Допиши в конец записи по шаблону:
   ```markdown
   ## HH:MM — <короткое название сделанного>
   - Что сделано: <1-3 предложения>
   - Файлы: <изменённые/созданные файлы>
   - Решения/примечания: <если были — иначе "—">
   ```
3. Не спрашивать разрешения каждый раз — это стандартное действие каждой сессии.

Такое правило работает надёжно только если лежит в `CLAUDE.md` в корне репозитория — Claude Code читает этот файл автоматически в начале каждой сессии, без напоминания со стороны пользователя.

---

## 38. Текущий прогресс проекта

> Этот раздел — временный трекер. Если в проекте уже используется отдельный `PROGRESS.md`, обновлять нужно там, а не здесь.

**Статус:** структура backend реализована (Phase 1: Domain, Infrastructure, репозитории, все 30 сущностей) — см. `docs/PROGRESS.md`, `docs/phase-summaries/`, `backend/progress/`.

**Завершённые этапы (раздел 23):** Этап 1 частично (entity/EF/репозитории готовы, Result pattern/exception middleware/Seeder — ещё нет).

**Текущий этап:** Этап 1. Основа проекта

**Известные решения/отклонения от ТЗ:**

- ⚠️ **Открытый вопрос**: `FarmerVerificationStatus`/`ListingStatus` в коде (уже смигрировано) используют упрощённые значения (`Pending/Verified/Rejected`, `Draft/Active/OutOfStock/Archived`), которые пользователь дал отдельным текстом в чате — но этот файл (каноническая версия ТЗ) содержит оригинальные значения (`Pending/Approved/Rejected/Suspended`, `Draft/Active/Hidden/OutOfStock/Blocked`). Требуется явное решение пользователя, какой вариант финальный — см. `backend/progress/2026-07-16.md`.
- Frontend — React вместо Blazor (раздел 12, уже не отклонение, а часть самого ТЗ).
- `CLAUDE.md` в корне репозитория реализован по разделу 37.4.
- **AI-ассистент (Anthropic Claude API)** — осознанное отклонение от раздела 3
  («В MVP не входят: искусственный интеллект»), подтверждено пользователем
  явно. Ассистент по тексту запроса покупателя ищет товар/раздел каталога
  через `search_products` tool и возвращает `{intent, productId, categoryId,
  message}` для навигации на фронтенде. Endpoint: `POST /api/ai-assistant/ask`.
  Модель — `claude-sonnet-5` (в присланном промпте было выдуманное имя
  `claude-sonnet-4-6` — исправлено). Ключ — `Anthropic:ApiKey`, пустая строка
  в `appsettings.json`, реальное значение — только в User Secrets.
