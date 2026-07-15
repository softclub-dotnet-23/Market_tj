# API — REST Conventions

> Источник: TZ_MarketTJ_ClaudeCode.md, разделы 13, 37.3.

Base URL: `/api` (без версии — раздел 13 задаёт полный список endpoint'ов без префикса `/v1`; пример `/api/v1/...` в разделе 37.3 — иллюстративный шаблон формата документа, а не отдельное решение о версионировании. Если версионирование понадобится позже — обновить этот файл и раздел 13/37.3 одновременно).

Все endpoint требуют JWT, кроме `/api/auth/login` и `/api/auth/register/*`.

## Conventions

* Versioning: не используется в MVP (см. примечание выше).
* JSON only, camelCase.
* DateTimes: ISO 8601 UTC.
* Money (`RetailPricePerKg` и т.п.): `decimal`, 2 знака (раздел 18).
* IDs: GUID (раздел 37.3).
* Pagination: `?pageNumber=&pageSize=` → `{ items, pageNumber, pageSize, totalCount, totalPages }` (раздел 20).
* Sorting/Filtering: раздел 13.5 (`search, categoryId, region` и т.д.).

## Формат ответа (раздел 20 + 20.1)

Формат `isSuccess/message/data/errorCode` — не заменять на ProblemDetails/RFC 7807 без отдельного согласования.

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

### Auth

```http
POST /api/auth/register/customer
POST /api/auth/register/farmer
POST /api/auth/login
GET  /api/auth/me
POST /api/auth/change-password
```

### Farmers

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

### Categories

```http
GET    /api/categories
GET    /api/categories/{id}
POST   /api/categories
PUT    /api/categories/{id}
DELETE /api/categories/{id}
```

Create/Update/Delete — только Admin.

### Products

```http
GET    /api/products
GET    /api/products/{id}
POST   /api/products
PUT    /api/products/{id}
DELETE /api/products/{id}
```

Управление — только Admin.

### Product Listings

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

Фильтры `GET /api/listings`: `search, categoryId, farmerId, region, district, minPrice, maxPrice, availableOnly, sortBy, pageNumber, pageSize`.

### Cart

```http
GET    /api/cart
POST   /api/cart/items
PUT    /api/cart/items/{id}
DELETE /api/cart/items/{id}
DELETE /api/cart/clear
```

### Orders

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

### Deliveries

```http
GET    /api/admin/couriers/available
POST   /api/admin/deliveries/assign

GET    /api/courier/deliveries/my
GET    /api/courier/deliveries/{id}
PATCH  /api/courier/deliveries/{id}/pickup
PATCH  /api/courier/deliveries/{id}/start
PATCH  /api/courier/deliveries/{id}/deliver
```

`GET /api/admin/couriers/available` — курьеры с `IsAvailable=true`, `IsActive=true`, с учётом региона/типа транспорта (раздел 10.5). Courier mini-interface (2 страницы) использует только 5 endpoint'ов из этой группы (кроме двух `admin/*`).

### Reviews

```http
POST   /api/reviews
GET    /api/farmers/{farmerId}/reviews
GET    /api/reviews/my
DELETE /api/reviews/{id}
```

Удаление доступно владельцу или Admin.

### Notifications

```http
GET    /api/notifications
PATCH  /api/notifications/{id}/read
PATCH  /api/notifications/read-all
```
