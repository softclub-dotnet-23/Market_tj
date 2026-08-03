import { useState, useEffect } from "react";
import { apiGet, apiPost, apiPut } from "@/lib/api";
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

export interface CreateCourierProfilePayload {
  userId: number;
  transportType: string;
  vehicleNumber: string;
  region: string;
  district: string;
}

// Регистрация курьера (Register.tsx) — тот же приём, что createFarmerProfile/
// createCustomerProfile: аккаунт уже создан и токен выдан отдельным шагом
// (POST /auth/register), здесь только заполняем профиль. isAvailable=false —
// новый курьер по умолчанию не в сети, пока сам не включит в своей панели;
// isActive=true — профиль сразу рабочий (не заблокирован).
export function createCourierProfile(payload: CreateCourierProfilePayload) {
  return apiPost<string>("/courier-profiles", { ...payload, isAvailable: false, isActive: true });
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

// PUT /courier-profiles/{id} — общий (админ+курьер) эндпоинт, требует ВЕСЬ
// объект, а не только изменённое поле. Собираем payload из уже загруженного
// профиля и меняем только isAvailable — не даём вызывающему коду случайно
// задеть Region/TransportType/IsActive при переключении доступности.
export function setCourierAvailability(profile: CourierProfileDto, isAvailable: boolean) {
  return apiPut<string>(`/courier-profiles/${profile.id}`, {
    id: profile.id,
    userId: profile.userId,
    transportType: profile.transportType,
    vehicleNumber: profile.vehicleNumber,
    region: profile.region,
    district: profile.district,
    isAvailable,
    isActive: profile.isActive,
  });
}
