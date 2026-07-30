import { useEffect, useMemo, useState } from "react";
import { apiGet, apiPost, apiPut } from "@/lib/api";
import { useAuth } from "@/context/AuthContext";

export const CustomerType = { Retail: 1, Wholesale: 2 } as const;

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

export interface CustomerProfileDto {
  id: number;
  userId: number;
  customerType: number;
  defaultAddress: string | null;
  region: string;
  district: string;
  createdAt: string;
  updatedAt: string;
}

export interface CustomerOrderDto {
  id: number;
  orderNumber: string;
  customerId: number;
  farmerId: number;
  status: number;
  deliveryAddress: string;
  region: string;
  district: string;
  customerComment: string | null;
  subtotal: number;
  deliveryPrice: number;
  totalAmount: number;
  createdAt: string;
  acceptedAt: string | null;
  completedAt: string | null;
  cancelledAt: string | null;
}

interface AsyncState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

function useAsync<T>(fetcher: () => Promise<T>, deps: unknown[]): AsyncState<T> {
  const [state, setState] = useState<AsyncState<T>>({ data: null, loading: true, error: null });

  useEffect(() => {
    let cancelled = false;
    setState({ data: null, loading: true, error: null });

    fetcher()
      .then((data) => {
        if (!cancelled) setState({ data, loading: false, error: null });
      })
      .catch((err: unknown) => {
        if (!cancelled) setState({ data: null, loading: false, error: err instanceof Error ? err.message : String(err) });
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  return state;
}

// /api/customer-profiles не фильтрует по userId на бэкенде (CurrentUserService
// — Этап 2, ещё не реализован) — тот же приём, что и для фермера: берём весь
// список и сопоставляем вошедшего пользователя на фронте.
export function useCustomerProfile(refreshKey = 0) {
  const { user } = useAuth();
  const { data: profiles, loading, error } = useAsync(() => apiGet<CustomerProfileDto[]>("/customer-profiles"), [refreshKey]);
  const profile = profiles?.find((p) => p.userId === user?.userId) ?? null;
  return { profile, loading, error };
}

// /api/orders тоже отдаёт все заказы без фильтра — фильтруем по customerId
// (это CustomerProfile.Id, не User.Id) на фронте.
export function useCustomerOrders(customerProfileId: number | null) {
  const { data, loading, error } = useAsync(
    () => (customerProfileId ? apiGet<CustomerOrderDto[]>("/orders") : Promise.resolve(null as never)),
    [customerProfileId],
  );
  const orders = useMemo(
    () =>
      data
        ?.filter((o) => o.customerId === customerProfileId)
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()) ?? null,
    [data, customerProfileId],
  );
  return { orders, loading, error };
}

export interface CreateCustomerProfilePayload {
  userId: number;
  region: string;
  district: string;
  defaultAddress: string | null;
}

// Второй шаг после POST /auth/register (раздел 23 ТЗ, Этап 3) — профиль
// создаётся отдельным вызовом уже с токеном, выданным при регистрации.
export function createCustomerProfile(payload: CreateCustomerProfilePayload) {
  return apiPost<string>("/customer-profiles", { ...payload, customerType: CustomerType.Retail });
}

export interface CustomerProfileEditableFields {
  region: string;
  district: string;
  defaultAddress: string | null;
}

// customerType сознательно не даём редактировать отсюда (переключение
// розница/опт — не самообслуживание, а отдельная бизнес-логика) — переносим
// его из уже загруженного профиля без изменений, как и userId.
export function updateCustomerProfile(current: CustomerProfileDto, edits: CustomerProfileEditableFields) {
  return apiPut<string>(`/customer-profiles/${current.id}`, {
    id: current.id,
    userId: current.userId,
    customerType: current.customerType,
    region: edits.region,
    district: edits.district,
    defaultAddress: edits.defaultAddress,
  });
}

export interface OrderItemPayload {
  productListingId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
}

export interface CreateOrderGroupPayload {
  farmerId: number;
  farmerUserId: number;
  region: string;
  district: string;
  deliveryAddress: string;
  customerComment: string | null;
  items: OrderItemPayload[];
}

// У Order — один FarmerId (раздел 8.11 ТЗ), поэтому если корзина собрала
// товары от нескольких фермеров, на каждого фермера уходит отдельный заказ —
// checkout группирует позиции корзины по farmerId перед вызовом этой функции.
export async function submitCustomerOrder(customerProfileId: number, payload: CreateOrderGroupPayload) {
  const subtotal = payload.items.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0);
  const orderNumber = `MTJ-${Date.now().toString(36).toUpperCase()}-${payload.farmerId}`;

  await apiPost<string>("/orders", {
    orderNumber,
    customerId: customerProfileId,
    farmerId: payload.farmerId,
    status: OrderStatus.Pending,
    deliveryAddress: payload.deliveryAddress,
    region: payload.region,
    district: payload.district,
    customerComment: payload.customerComment,
    subtotal,
    deliveryPrice: 0,
    totalAmount: subtotal,
    acceptedAt: null,
    completedAt: null,
    cancelledAt: null,
  });

  // POST /api/orders не возвращает id созданной записи (та же схема, что и у
  // ProductListing/SupportTicket на бэкенде) — достаём id только что
  // созданного заказа через GET, находя его по customerId+уникальному
  // orderNumber, который сами сгенерировали чуть выше.
  const orders = await apiGet<CustomerOrderDto[]>("/orders");
  const created = orders.find((o) => o.customerId === customerProfileId && o.orderNumber === orderNumber);
  if (!created) throw new Error("Заказ создан, но не найден для добавления позиций");

  for (const item of payload.items) {
    await apiPost<string>("/order-items", {
      orderId: created.id,
      productListingId: item.productListingId,
      productName: item.productName,
      unitPrice: item.unitPrice,
      quantity: item.quantity,
      totalPrice: item.unitPrice * item.quantity,
    });
  }

  // Уведомление — вспомогательный сигнал, а не часть самого заказа: если оно
  // не отправится (например, сеть моргнула), заказ всё равно должен считаться
  // оформленным, поэтому ошибку тут не пробрасываем дальше.
  try {
    await apiPost<string>("/notifications", {
      userId: payload.farmerUserId,
      title: "Новый заказ",
      message: `У вас новый заказ ${orderNumber} на сумму ${subtotal} сомони`,
      isRead: false,
    });
  } catch (err) {
    console.error("Не удалось отправить уведомление фермеру о новом заказе", err);
  }

  return created;
}

export interface CustomerReviewDto {
  id: number;
  orderId: number;
  customerId: number;
  farmerId: number;
  rating: number;
  comment: string | null;
  createdAt: string;
}

// Раздел 10.6 ТЗ: отзыв можно оставить только на свой Completed-заказ, и по
// одному заказу — только один отзыв (бэкенд это тоже проверяет, см.
// ReviewService.CreateAsync — тут просто заранее знаем, что уже оставлено,
// чтобы не показывать кнопку "Оставить отзыв" повторно).
export function useCustomerReviewedOrderIds(customerProfileId: number | null, refreshKey = 0) {
  const { data, loading } = useAsync(
    () => (customerProfileId ? apiGet<CustomerReviewDto[]>("/reviews") : Promise.resolve(null as never)),
    [customerProfileId, refreshKey],
  );
  const reviewedOrderIds = new Set(data?.filter((r) => r.customerId === customerProfileId).map((r) => r.orderId) ?? []);
  return { reviewedOrderIds, loading };
}

export interface CreateReviewPayload {
  orderId: number;
  farmerId: number;
  rating: number;
  comment: string | null;
}

export function submitCustomerReview(customerProfileId: number, payload: CreateReviewPayload) {
  return apiPost<string>("/reviews", {
    orderId: payload.orderId,
    customerId: customerProfileId,
    farmerId: payload.farmerId,
    rating: payload.rating,
    comment: payload.comment,
  });
}
