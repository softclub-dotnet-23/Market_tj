import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api";
import { useAuth } from "@/context/AuthContext";

export const ListingStatus = { Draft: 1, Active: 2, OutOfStock: 3, Archived: 4 } as const;
export const FarmerVerificationStatus = { Pending: 1, Verified: 2, Rejected: 3 } as const;

export interface FarmerProfileDto {
  id: number;
  userId: number;
  farmName: string;
  region: string;
  district: string;
  village: string;
  address: string;
  description: string | null;
  verificationStatus: number;
  verifiedAt: string | null;
  verifiedByAdminId: number | null;
  createdAt: string;
  updatedAt: string;
}

export interface TopSellingProductDto {
  productName: string;
  quantitySold: number;
  revenue: number;
}

export interface MonthlyRevenueDto {
  year: number;
  month: number;
  revenue: number;
}

export interface FarmerDashboardDto {
  totalOwnProducts: number;
  activeProducts: number;
  totalOrdersReceived: number;
  ordersThisMonth: number;
  totalRevenue: number;
  revenueThisMonth: number;
  topSellingOwnProducts: TopSellingProductDto[];
  revenueByMonth: MonthlyRevenueDto[];
  averageRating: number | null;
}

export interface ProductListingDto {
  id: number;
  farmerProfileId: number;
  productId: number;
  title: string;
  description: string | null;
  retailPricePerKg: number;
  wholesalePricePerKg: number | null;
  wholesaleMinimumQuantity: number | null;
  availableQuantity: number;
  minimumOrderQuantity: number;
  harvestDate: string | null;
  expectedHarvestDate: string | null;
  qualityGrade: string;
  region: string;
  district: string;
  address: string;
  status: number;
  createdAt: string;
  updatedAt: string;
}

interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CatalogProductDto {
  id: number;
  categoryId: number;
  name: string;
  description: string | null;
  unit: string;
  isActive: boolean;
}

export interface CreateProductListingDto {
  farmerProfileId: number;
  productId: number;
  title: string;
  description?: string | null;
  retailPricePerKg: number;
  wholesalePricePerKg?: number | null;
  wholesaleMinimumQuantity?: number | null;
  availableQuantity: number;
  minimumOrderQuantity: number;
  harvestDate?: string | null;
  expectedHarvestDate?: string | null;
  qualityGrade: string;
  region: string;
  district: string;
  address: string;
  status: number;
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

// Бэкенд ещё не умеет фильтровать /api/farmer-profiles по userId (раздел 23,
// CurrentUserService не реализован) — забираем весь список и сопоставляем
// вошедшего пользователя на фронте.
export function useFarmerProfile() {
  const { user } = useAuth();
  const { data: profiles, loading, error } = useAsync(() => apiGet<FarmerProfileDto[]>("/farmer-profiles"), []);
  const profile = profiles?.find((p) => p.userId === user?.userId) ?? null;
  return { profile, loading, error };
}

export function useFarmerDashboard(farmerProfileId: number | null) {
  return useAsync(
    () => (farmerProfileId ? apiGet<FarmerDashboardDto>(`/analytics/farmer/dashboard?farmerId=${farmerProfileId}`) : Promise.resolve(null as never)),
    [farmerProfileId],
  );
}

// Тот же пробел, что и с профилем: /api/product-listings не фильтрует по
// farmerId на бэкенде, поэтому берём достаточно большую страницу и
// отфильтровываем/пагинируем товары фермера на фронте.
export function useFarmerProducts(farmerProfileId: number | null, refreshKey = 0) {
  const { data, loading, error } = useAsync(
    () => (farmerProfileId ? apiGet<PagedResultDto<ProductListingDto>>("/product-listings?pageNumber=1&pageSize=200") : Promise.resolve(null as never)),
    [farmerProfileId, refreshKey],
  );
  const products = data?.items.filter((p) => p.farmerProfileId === farmerProfileId) ?? null;
  return { products, loading, error };
}

// Каталог базовых товаров (/api/products) — из него фермер выбирает
// productId при создании нового объявления.
export function useProductCatalog() {
  const { data, loading, error } = useAsync(() => apiGet<CatalogProductDto[]>("/products"), []);
  return { catalog: data?.filter((p) => p.isActive) ?? null, loading, error };
}

export function createProductListing(dto: CreateProductListingDto) {
  return apiPost<string>("/product-listings", dto);
}
