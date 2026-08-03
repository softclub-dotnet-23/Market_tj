namespace MarketTJ.Domain.Enums;

// Способ оплаты ЗАКАЗА (гибридная оплата: своя виртуальная карта из Wallet
// или наличные курьеру). Сознательно отдельный enum от Domain.Enums.PaymentMethod
// (Cash/MobileMoney/Card) — тот используется отдельной, не связанной с этим
// потоком сущностью Payment (ручной CRUD-журнал платежей, см. PaymentService/
// PaymentController), которая не подключена к оформлению заказа и Wallet.
// Смешивать их — только усложнить оба независимых куска функциональности.
public enum OrderPaymentMethod
{
    Card = 1,
    CashOnDelivery = 2
}
