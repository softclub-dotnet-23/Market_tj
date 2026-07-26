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

// Раздел заказа у фермера — только начало цепочки (раздел 8.11 ТЗ: Order одного
// FarmerId, но дальше доставку и статусы после сборки ведёт Admin/курьер).
// Фермер принимает/отклоняет заказ и готовит его, а с "Готов к выдаче" эстафету
// перенимает Admin (назначение курьера и всё, что после).
export function getFarmerNextStatuses(current: number): number[] {
  switch (current) {
    case OrderStatus.Pending:
      return [OrderStatus.Accepted, OrderStatus.Rejected];
    case OrderStatus.Accepted:
      return [OrderStatus.Preparing];
    case OrderStatus.Preparing:
      return [OrderStatus.ReadyForPickup];
    default:
      return [];
  }
}

// Зеркально getFarmerNextStatuses: пока заказ не дошёл до "Готов к выдаче",
// это целиком зона фермера — Admin не должен иметь возможность вмешаться
// (в том числе перепрыгнуть статус или случайно всё сломать). С "Готов к
// выдаче" эстафету перенимает Admin — назначение курьера и всё дальше по
// цепочке доставки, плюс отмена на любом из этих шагов.
export function getAdminNextStatuses(current: number): number[] {
  switch (current) {
    case OrderStatus.ReadyForPickup:
      return [OrderStatus.CourierAssigned, OrderStatus.Cancelled];
    case OrderStatus.CourierAssigned:
      return [OrderStatus.PickedUp, OrderStatus.Cancelled];
    case OrderStatus.PickedUp:
      return [OrderStatus.InDelivery, OrderStatus.Cancelled];
    case OrderStatus.InDelivery:
      return [OrderStatus.Delivered, OrderStatus.Cancelled];
    case OrderStatus.Delivered:
      return [OrderStatus.Completed];
    default:
      return [];
  }
}
