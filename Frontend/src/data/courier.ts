import { useState, useEffect } from "react";
import { apiGet } from "@/lib/api";
import { useAuth } from "@/context/AuthContext";

export interface CourierProfileDto {
  id: number;
  userId: number;
  transportType: string;
  vehicleNumber: string;
  region: string;
  district: string;
  isAvailable: boolean;
  isActive: boolean;
  rating: number;
  createdAt: string;
  updatedAt: string;
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

// /courier-profiles (GetAll) уже фильтрует на бэкенде до "только свой профиль"
// для не-админа (см. CourierProfileService.GetAllAsync) — тот же приём, что
// useFarmerProfile у фермера.
export function useCourierProfile(refreshKey = 0) {
  const { user } = useAuth();
  const { data: profiles, loading, error } = useAsync(() => apiGet<CourierProfileDto[]>("/courier-profiles"), [refreshKey]);
  const profile = profiles?.find((p) => p.userId === user?.userId) ?? null;
  return { profile, loading, error };
}
