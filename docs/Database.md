# Database

> Источник: TZ_MarketTJ_ClaudeCode.md, разделы 8, 9.

## Основные сущности

### User

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

* Email уникальный;
* PhoneNumber уникальный;
* FullName обязательный;
* пользователь может быть заблокирован.

### FarmerProfile

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

Связь: `User 1 — 1 FarmerProfile`.

### CustomerProfile

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

### CourierProfile

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

`Region`/`District`/`IsActive` добавлены, чтобы Admin мог осознанно выбирать курьера при назначении (см. [DesignerLogic.md](DesignerLogic.md) — «Доставка»).

### Category

```text
Id
Name
Description
ImageUrl
IsActive
CreatedAt
UpdatedAt
```

Примеры: Овощи, Фрукты, Зелень, Сухофрукты, Орехи, Молочная продукция.

### Product

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

Примеры: Помидор, Картофель, Лук, Яблоко, Виноград.

### ProductListing

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

* цена больше 0;
* количество не меньше 0;
* минимальный заказ больше 0;
* оптовая цена не должна быть выше розничной;
* объявление принадлежит одному фермеру;
* неподтверждённый фермер не может создать активное объявление.

### ProductImage

```text
Id
ProductListingId
ImageUrl
IsMain
CreatedAt
```

Ограничения:

* форматы: jpg, jpeg, png, webp;
* максимальный размер: 5 MB;
* максимум 5 изображений на объявление.

### CartItem

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

* один и тот же продукт не должен повторяться;
* количество должно быть больше 0;
* количество не должно превышать доступный остаток.

### Order

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

Для MVP один заказ должен содержать товары только одного фермера. Если в корзине товары разных фермеров, система создаёт отдельный заказ для каждого фермера.

### OrderItem

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

Важно сохранять копию названия товара, цены и количества. Изменение цены объявления не должно менять старый заказ.

Формула: `TotalPrice = UnitPrice × Quantity`.

### Delivery

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

### Review

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

* рейтинг от 1 до 5;
* отзыв создаётся только после завершения заказа;
* по одному заказу можно оставить только один отзыв;
* клиент может оставить отзыв только на свой заказ.

### Notification

```text
Id
UserId
Title
Message
IsRead
CreatedAt
```

Примеры: новый заказ, заказ принят, заказ отклонён, назначен курьер, заказ доставлен, фермер подтверждён, фермер отклонён.

## Связи сущностей

```text
User 1 — 1 FarmerProfile
User 1 — 1 CustomerProfile
User 1 — 1 CourierProfile

Category 1 — many Product
Product 1 — many ProductListing
FarmerProfile 1 — many ProductListing
ProductListing 1 — many ProductImage

CustomerProfile 1 — many CartItem
ProductListing 1 — many CartItem

CustomerProfile 1 — many Order
FarmerProfile 1 — many Order
Order 1 — many OrderItem
ProductListing 1 — many OrderItem

Order 1 — 0..1 Delivery
CourierProfile 1 — many Delivery

Order 1 — 0..1 Review
FarmerProfile 1 — many Review
```

## Общие требования к данным (см. также раздел 18, 37.3 ТЗ)

* **Id всех сущностей — int** (autoincrement; исправлено пользователем, раздел 37.3 ТЗ обновлён с GUID на int);
* денежные значения — `decimal`, `HasPrecision(18, 2)`;
* количество — `decimal`, `HasPrecision(18, 3)`;
* общие поля сущностей: `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`;
* для важных данных рекомендуется soft delete.
