import {
  Award,
  Ban,
  CheckCircle2,
  ChefHat,
  Clock,
  Home,
  Package,
  PackageCheck,
  Truck,
  UserCheck,
  XCircle,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";

export const OrderStatus = {
  Pending: 1,
  Accepted: 2,
  Rejected: 3,
  Preparing: 4,
  ReadyForPickup: 5,
  CourierAssigned: 6,
  PickedUp: 7,
  InDelivery: 8,
  Delivered: 9,
  Completed: 10,
  Cancelled: 11,
} as const;

export const ORDER_STATUS_CLASSES: Record<number, string> = {
  [OrderStatus.Pending]: "bg-stone-100 text-stone-600 dark:bg-stone-800 dark:text-stone-300",
  [OrderStatus.Accepted]: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  [OrderStatus.Rejected]: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
  [OrderStatus.Preparing]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [OrderStatus.ReadyForPickup]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [OrderStatus.CourierAssigned]: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  [OrderStatus.PickedUp]: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  [OrderStatus.InDelivery]: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  [OrderStatus.Delivered]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [OrderStatus.Completed]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [OrderStatus.Cancelled]: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
};

export const ORDER_STATUS_KEYS: Record<number, string> = {
  [OrderStatus.Pending]: "pending",
  [OrderStatus.Accepted]: "accepted",
  [OrderStatus.Rejected]: "rejected",
  [OrderStatus.Preparing]: "preparing",
  [OrderStatus.ReadyForPickup]: "readyForPickup",
  [OrderStatus.CourierAssigned]: "courierAssigned",
  [OrderStatus.PickedUp]: "pickedUp",
  [OrderStatus.InDelivery]: "inDelivery",
  [OrderStatus.Delivered]: "delivered",
  [OrderStatus.Completed]: "completed",
  [OrderStatus.Cancelled]: "cancelled",
};

export const ORDER_STATUS_ICONS: Record<number, LucideIcon> = {
  [OrderStatus.Pending]: Clock,
  [OrderStatus.Accepted]: CheckCircle2,
  [OrderStatus.Rejected]: XCircle,
  [OrderStatus.Preparing]: ChefHat,
  [OrderStatus.ReadyForPickup]: PackageCheck,
  [OrderStatus.CourierAssigned]: UserCheck,
  [OrderStatus.PickedUp]: Package,
  [OrderStatus.InDelivery]: Truck,
  [OrderStatus.Delivered]: Home,
  [OrderStatus.Completed]: Award,
  [OrderStatus.Cancelled]: Ban,
};

// Раздельные статусы фермера/курьера (Order.FarmerStatus/CourierStatus) —
// независимые от вычисляемого combined `status` выше. См.
// OrderService.ComputeDisplayStatus на бэкенде и docs про item 1 (2026-08-05):
// эти поля уже приходили с бэкенда, но фронтенд их нигде не показывал явно.
export const FarmerOrderStatus = {
  Accepted: 1,
  HandedToCourier: 2,
} as const;

export const CourierOrderStatus = {
  Accepted: 1,
  Delivered: 2,
} as const;

export const FARMER_ORDER_STATUS_KEYS: Record<number, string> = {
  [FarmerOrderStatus.Accepted]: "accepted",
  [FarmerOrderStatus.HandedToCourier]: "handedToCourier",
};

export const COURIER_ORDER_STATUS_KEYS: Record<number, string> = {
  [CourierOrderStatus.Accepted]: "accepted",
  [CourierOrderStatus.Delivered]: "delivered",
};

export const ALL_ORDER_STATUSES: number[] = [
  OrderStatus.Pending,
  OrderStatus.Accepted,
  OrderStatus.Rejected,
  OrderStatus.Preparing,
  OrderStatus.ReadyForPickup,
  OrderStatus.CourierAssigned,
  OrderStatus.PickedUp,
  OrderStatus.InDelivery,
  OrderStatus.Delivered,
  OrderStatus.Completed,
  OrderStatus.Cancelled,
];

// "Получен" в идеале — это Delivery.DeliveredAt (реальный факт доставки
// курьером), но в проекте пока нет ни курьерского кабинета, ни админ-формы,
// которая создавала бы/обновляла запись Delivery — поэтому эта запись сейчас
// никогда не появляется, даже когда Admin вручную закрывает заказ статусом
// Completed. Честный фолбэк: раз Order.CompletedAt бэкенд и так проставляет
// сам при первом переходе в Completed (см. OrderService.UpdateAsync), это
// ближайший реальный сигнал "когда заказ закрыли как полученный", пока
// настоящего курьерского флоу нет — используем его, если записи о доставке
// действительно нет.
export function resolveReceivedAt(status: number, completedAt: string | null, deliveredAt: string | null | undefined): string | null {
  if (deliveredAt) return deliveredAt;
  if (status === OrderStatus.Completed && completedAt) return completedAt;
  return null;
}

// По прямому запросу пользователя (2026-08-05): фермер только принимает или
// отклоняет заказ — "Принял", и всё, дальше статус вручную не меняется.
// Готовность к выдаче/сборка больше не отдельный шаг: как только фермер
// назначает курьера (см. AssignCourierDrawer), это и есть сигнал "заказ
// готов" — Preparing/ReadyForPickup из старой цепочки убраны намеренно.
export function getFarmerNextStatuses(current: number): number[] {
  switch (current) {
    case OrderStatus.Pending:
      return [OrderStatus.Accepted, OrderStatus.Rejected];
    default:
      return [];
  }
}

// По прямому запросу пользователя (2026-08-05): Admin больше не участвует в
// приёме заказа и назначении курьера (это фермер, см. getFarmerNextStatuses/
// AssignCourierDrawer) — реальная доставка отслеживается отдельно через
// Delivery.Status/CourierStatus, не через Order.Status. С этой же даты заказ
// завершается САМ, как только доставка подтверждена (см. DeliveryService.
// CompleteOrderAfterDeliveryAsync на бэкенде) — Admin для этого обычно не
// нужен. Но Order.Status у заказов, уже дошедших до Delivered/промежуточных
// статусов ДО этого исправления (или если автозавершение почему-то не
// сработало), должен оставаться финализируемым вручную — иначе такой заказ
// зависает навсегда без отзыва/начисления фермеру.
export function getAdminNextStatuses(current: number): number[] {
  switch (current) {
    case OrderStatus.Accepted:
    case OrderStatus.Preparing:
    case OrderStatus.ReadyForPickup:
    case OrderStatus.CourierAssigned:
    case OrderStatus.PickedUp:
    case OrderStatus.InDelivery:
    case OrderStatus.Delivered:
      return [OrderStatus.Completed, OrderStatus.Cancelled];
    default:
      return [];
  }
}
