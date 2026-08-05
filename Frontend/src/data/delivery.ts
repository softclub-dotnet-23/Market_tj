import { useEffect, useState } from "react";
import { apiGet, apiPatch, apiPost, apiUpload } from "@/lib/api";

// Полноценное назначение и отслеживание курьера — по прямому запросу
// пользователя (2026-08-02). DeliveryStatus здесь — точное зеркало
// backend/MarketTJ.Domain/Enums/DeliveryStatus.cs.
export const DeliveryStatus = {
  Pending: 1,
  Assigned: 2,
  Accepted: 3,
  GoingToFarmer: 4,
  ArrivedAtFarmer: 5,
  PickedUp: 6,
  InTransit: 7,
  ArrivedAtClient: 8,
  Delivered: 9,
  Cancelled: 10,
} as const;

// Один разрешённый следующий шаг с текущего статуса — то же самое, что
// DeliveryService.CourierTransitions на бэкенде. Здесь только для UI (какую
// именно кнопку показать курьеру); реальная проверка — на сервере.
// Упрощение находки 2026-08-05 ("каждый шаг должен записать, это очень
// неудобно"): вместо 5 отдельных кнопок — одна, сразу после принятия
// доставки ("Забрал, еду к покупателю"). GoingToFarmer/ArrivedAtFarmer/
// PickedUp как источники — fallback для доставок, застрявших на них до
// упрощения (у них тоже должна остаться ровно одна кнопка "далее").
export const NEXT_COURIER_STATUS: Partial<Record<number, number>> = {
  [DeliveryStatus.Accepted]: DeliveryStatus.InTransit,
  [DeliveryStatus.GoingToFarmer]: DeliveryStatus.InTransit,
  [DeliveryStatus.ArrivedAtFarmer]: DeliveryStatus.InTransit,
  [DeliveryStatus.PickedUp]: DeliveryStatus.InTransit,
};

export interface DeliveryDto {
  id: number;
  orderId: number;
  courierId: number | null;
  // По прямому запросу пользователя (2026-08-05) — курьер "вручную", не
  // зарегистрированный на платформе: courierId в этом случае null.
  manualCourierName: string | null;
  manualCourierPhone: string | null;
  pickupAddress: string;
  deliveryAddress: string;
  deliveryPrice: number;
  status: number;
  estimatedPickupAt: string | null;
  estimatedDeliveryAt: string | null;
  assignedAt: string | null;
  acceptedAt: string | null;
  pickedUpAt: string | null;
  deliveredAt: string | null;
  cancelledAt: string | null;
  farmerNote: string | null;
  clientNote: string | null;
  adminNote: string | null;
  courierNote: string | null;
  cancellationReason: string | null;
  problemDescription: string | null;
  createdAt: string;
  updatedAt: string;
  courierFullName: string | null;
  courierPhoneNumber: string | null;
  courierAvatarUrl: string | null;
  courierTransportType: string | null;
  courierVehicleNumber: string | null;
  courierRating: number | null;
  // Фото подтверждения доставки вместо кода (2026-08-05) — загружает
  // курьер (или фермер для доставки "вручную") при confirmDelivery/
  // confirmManualDelivery, видно покупателю/фермеру/курьеру.
  deliveryProofPhotoUrl: string | null;
  orderNumber: string | null;
  farmerName: string | null;
  farmerPhoneNumber: string | null;
  customerName: string | null;
  customerPhoneNumber: string | null;
  itemCount: number;
  // Order.FarmerStatus/CourierStatus — раздельные статусы (см. FarmerOrderDto
  // в data/farmer.ts, тот же принцип).
  farmerStatus: number | null;
  courierStatus: number | null;
}

export interface AvailableCourierDto {
  id: number;
  userId: number;
  fullName: string;
  phoneNumber: string;
  avatarUrl: string | null;
  transportType: string;
  vehicleNumber: string;
  region: string;
  district: string;
  rating: number;
  isAvailable: boolean;
  activeDeliveries: number;
  completedDeliveries: number;
  // Реальное расстояние (Haversine) от курьера до адреса доставки заказа —
  // список уже отфильтрован по ≤40 км и отсортирован по возрастанию на
  // бэкенде (см. DeliveryService.GetAvailableCouriersAsync, 2026-08-05).
  distanceKm: number;
}

export interface AssignCourierPayload {
  courierId: number;
  deliveryFee: number;
  estimatedPickupAt: string | null;
  estimatedDeliveryAt: string | null;
  adminNote: string | null;
}

export interface UpdateDeliveryAdminDetailsPayload {
  deliveryFee: number;
  estimatedPickupAt: string | null;
  estimatedDeliveryAt: string | null;
  adminNote: string | null;
}

export interface AssignManualCourierPayload {
  courierName: string;
  courierPhone: string;
  deliveryFee: number;
  estimatedPickupAt: string | null;
  estimatedDeliveryAt: string | null;
  adminNote: string | null;
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
        if (!cancelled) {
          setState({ data: null, loading: false, error: err instanceof Error ? err.message : String(err) });
        }
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  return state;
}

export function useDeliveryByOrder(orderId: number | null, refreshKey = 0) {
  const { data, loading, error } = useAsync(
    () => (orderId ? apiGet<DeliveryDto | null>(`/deliveries/by-order/${orderId}`) : Promise.resolve(null)),
    [orderId, refreshKey],
  );
  return { delivery: data, loading, error };
}

export function useMyDeliveries(refreshKey = 0) {
  const { data, loading, error } = useAsync(() => apiGet<DeliveryDto[]>("/deliveries/my"), [refreshKey]);
  return { deliveries: data, loading, error };
}

export interface AvailableCourierFilters {
  orderId: number;
  onlyAvailable?: boolean;
  region?: string;
  district?: string;
  transportType?: string;
  minRating?: number;
}

// Region/District — реальный опциональный фильтр (см.
// DeliveryService.GetAvailableCouriersAsync). orderId обязателен — сервер
// резолвит координаты адреса доставки этого заказа (геокодирует и кэширует
// лениво) и по ним же жёстко фильтрует курьеров в радиусе 40 км (2026-08-05,
// заменяет прежнюю сортировку "тот же район первым").
export function useAvailableCouriers(filters: AvailableCourierFilters, enabled: boolean, refreshKey = 0) {
  const query = new URLSearchParams();
  query.set("orderId", String(filters.orderId));
  if (filters.onlyAvailable) query.set("onlyAvailable", "true");
  if (filters.region) query.set("region", filters.region);
  if (filters.district) query.set("district", filters.district);
  if (filters.transportType) query.set("transportType", filters.transportType);
  if (filters.minRating) query.set("minRating", String(filters.minRating));

  const { data, loading, error } = useAsync(
    () => (enabled ? apiGet<AvailableCourierDto[]>(`/deliveries/available-couriers?${query.toString()}`) : Promise.resolve(null)),
    [enabled, filters.orderId, filters.onlyAvailable, filters.region, filters.district, filters.transportType, filters.minRating, refreshKey],
  );
  return { couriers: data, loading, error };
}

export function assignCourier(orderId: number, payload: AssignCourierPayload) {
  return apiPost<string>(`/deliveries/by-order/${orderId}/assign`, payload);
}

export function assignManualCourier(orderId: number, payload: AssignManualCourierPayload) {
  return apiPost<string>(`/deliveries/by-order/${orderId}/assign-manual`, payload);
}

// Фото вместо кода (2026-08-05) — тот же multipart-приём, что и
// uploadCourierDocument в data/courier.ts.
export function confirmManualDelivery(deliveryId: number, photo: File) {
  const formData = new FormData();
  formData.append("photo", photo);
  return apiUpload<string>(`/deliveries/${deliveryId}/confirm-manual`, formData);
}

export function updateDeliveryAdminDetails(deliveryId: number, payload: UpdateDeliveryAdminDetailsPayload) {
  return apiPatch<string>(`/deliveries/${deliveryId}/admin-details`, payload);
}

export function cancelDelivery(deliveryId: number, reason: string) {
  return apiPost<string>(`/deliveries/${deliveryId}/cancel`, { reason });
}

export function markReadyForPickup(orderId: number) {
  return apiPost<string>(`/deliveries/by-order/${orderId}/mark-ready`, {});
}

export function acceptDelivery(deliveryId: number) {
  return apiPost<string>(`/deliveries/${deliveryId}/accept`, {});
}

export function updateCourierDeliveryStatus(deliveryId: number, status: number, note?: string) {
  return apiPatch<string>(`/deliveries/${deliveryId}/status`, { status, note: note ?? null });
}

export function confirmDelivery(deliveryId: number, photo: File) {
  const formData = new FormData();
  formData.append("photo", photo);
  return apiUpload<string>(`/deliveries/${deliveryId}/confirm`, formData);
}

export function reportDeliveryProblem(deliveryId: number, description: string) {
  return apiPost<string>(`/deliveries/${deliveryId}/report-problem`, { description });
}
