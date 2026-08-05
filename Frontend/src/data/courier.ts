import { useState, useEffect } from "react";
import { apiGet, apiPost, apiPut, apiUpload, apiDelete } from "@/lib/api";
import { useAuth } from "@/context/AuthContext";
import { DocumentReviewStatus } from "@/data/farmer";

export interface CourierProfileDto {
  id: number;
  userId: number;
  transportType: string;
  vehicleNumber: string;
  region: string;
  district: string;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
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
  address: string;
}

// Регистрация курьера (Register.tsx) — тот же приём, что createFarmerProfile/
// createCustomerProfile: аккаунт уже создан и токен выдан отдельным шагом
// (POST /auth/register), здесь только заполняем профиль. isAvailable=false —
// новый курьер по умолчанию не в сети, пока сам не включит в своей панели;
// isActive=true — профиль сразу рабочий (не заблокирован). Возвращает Id
// созданного профиля (не строку с сообщением) — по прямому запросу
// пользователя (2026-08-04): форма регистрации сразу после этого грузит
// документы верификации, для чего нужен CourierProfileId.
export function createCourierProfile(payload: CreateCourierProfilePayload) {
  return apiPost<number>("/courier-profiles", { ...payload, isAvailable: false, isActive: true });
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

// Верификация документов курьера (2026-08-04) — зеркалит
// FarmerDocumentType/DocumentReviewStatus/hasRequiredFarmerDocuments из
// data/farmer.ts (DocumentReviewStatus переиспользуется оттуда напрямую, он
// уже общий). Гейт строже фермерского: там достаточно "не отклонён", здесь
// нужен именно Approved — курьер не может стать доступным для заказов, пока
// admin не одобрил оба документа (см. CourierProfileService.UpdateAsync).
export const CourierDocumentType = { DriverLicense: 1, VehicleRegistration: 2 } as const;

export const REQUIRED_COURIER_DOCUMENT_TYPES = [
  CourierDocumentType.DriverLicense,
  CourierDocumentType.VehicleRegistration,
] as const;

export interface CourierDocumentDto {
  id: number;
  courierProfileId: number;
  documentType: number;
  fileUrl: string;
  status: number;
  uploadedAt: string;
  reviewedAt: string | null;
  reviewedByAdminId: number | null;
  rejectionReason: string | null;
}

export function hasApprovedCourierDocuments(documents: CourierDocumentDto[] | null): boolean {
  if (!documents) return false;
  const approvedTypes = new Set(documents.filter((d) => d.status === DocumentReviewStatus.Approved).map((d) => d.documentType));
  return REQUIRED_COURIER_DOCUMENT_TYPES.every((type) => approvedTypes.has(type));
}

export function useCourierDocuments(courierProfileId: number | null, refreshKey = 0) {
  const { data, loading, error } = useAsync(
    () => (courierProfileId ? apiGet<CourierDocumentDto[]>("/courier-documents") : Promise.resolve(null as never)),
    [courierProfileId, refreshKey],
  );
  const documents = data?.filter((d) => d.courierProfileId === courierProfileId) ?? null;
  return { documents, loading, error };
}

export function uploadCourierDocument(courierProfileId: number, documentType: number, file: File) {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("courierProfileId", String(courierProfileId));
  formData.append("documentType", String(documentType));
  return apiUpload<CourierDocumentDto>("/courier-documents/upload", formData);
}

export function deleteCourierDocument(id: number) {
  return apiDelete<string>(`/courier-documents/${id}`);
}
