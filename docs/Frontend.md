# Frontend

> Источник: TZ_MarketTJ_ClaudeCode.md, разделы 12, 14, 15 (финальная версия — React указан в стеке изначально).

## Стек

React + TypeScript, React Router, Axios/fetch, React Context для состояния (auth/корзина). Подробности — [ArchitectureRules.md](ArchitectureRules.md).

## Структура интерфейса: 3 части + Courier mini-interface

Интерфейс — **не 4 полноценные панели**, а:

1. **Public/Customer Website** — Customer работает внутри публичного сайта, отдельной dashboard-панели у него нет;
2. **Farmer Panel** — полноценная панель;
3. **Admin Panel** — полноценная панель;
4. **Courier mini-interface** — 2 страницы, НЕ считается отдельной полноценной панелью.

## Страницы

### Public/Customer Website

Здесь же живёт Customer — отдельного `/customer/dashboard`/панели нет:

```text
/                 (Home)
/catalog          (Catalog)
/product/:id      (Product Details)
/cart             (Cart)
/checkout         (Checkout)
/orders           (My Orders)
/orders/:id       (My Orders → детали заказа)
/profile          (Profile)
/login
/register
/about
/contact
/forbidden
/not-found
```

### Farmer Panel

```text
/farmer                  (Dashboard)
/farmer/products          (My Products)
/farmer/products/create    (Create Product)
/farmer/products/:id/edit   (Edit Product)
/farmer/orders                (Orders)
/farmer/orders/:id
/farmer/reviews                 (Reviews)
/farmer/profile                  (Profile)
/farmer/notifications              (Notifications)
```

`/farmer` — Dashboard (главная страница панели). `/farmer/products/*` управляют сущностью `ProductListing` (раздел 8.7 ТЗ) — в UI слово «продукт/товар», в коде/БД сущность остаётся `ProductListing`, не переименовывать. Страница верификации фермера отдельно не создаётся — встроена в `/farmer/profile` (статус верификации + форма отправки на проверку показываются там же).

### Admin Panel

```text
/admin                (Dashboard)
/admin/users            (Users)
/admin/farmers            (Farmers)
/admin/farmers/pending      (Farmer Verification)
/admin/categories              (Categories)
/admin/products                  (Products)
/admin/orders                       (Orders)
/admin/couriers                        (Couriers)
/admin/deliveries                          (Deliveries)
/admin/reviews                                 (Reviews)
/admin/statistics                                 (Statistics)
```

`/admin/couriers` — список курьеров с `IsAvailable`/регионом/типом транспорта (для назначения, см. [Api.md](Api.md) — Deliveries).

### Courier mini-interface (НЕ полноценная 4-я панель)

```text
/courier/deliveries       (My Deliveries)
/courier/deliveries/:id    (Delivery Details)
```

Всего 2 страницы, без Dashboard, без отдельного layout с боковым меню. Courier может только: смотреть список назначенных доставок, открывать детали (адрес+телефон Farmer, адрес+телефон Customer), менять статус PickedUp → InDelivery → Delivered.

## Требования к интерфейсу

Интерфейс должен быть:

* адаптивным;
* удобным для телефона;
* простым;
* без перегруженных форм;
* с крупными кнопками;
* с понятными статусами;
* с таджикским языком (основной);
* с возможностью добавить русский язык (дополнительный);
* с подтверждением опасных действий;
* с сообщениями об ошибках;
* с индикаторами загрузки.

Для фермера особенно важно:

* минимум текста;
* короткие формы;
* понятные иконки;
* возможность загрузить фото с телефона;
* быстрое изменение цены и количества.

Для Courier (mini-interface) — минимум экранов и действий: список доставок и одна кнопка смены статуса, без лишней навигации.
