import { useEffect, useState } from "react";
import { apiGet } from "@/lib/api";
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
export function useCustomerProfile() {
  const { user } = useAuth();
  const { data: profiles, loading, error } = useAsync(() => apiGet<CustomerProfileDto[]>("/customer-profiles"), []);
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
  const orders = data
    ?.filter((o) => o.customerId === customerProfileId)
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()) ?? null;
  return { orders, loading, error };
}
