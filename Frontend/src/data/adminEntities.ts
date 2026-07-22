import { useEffect, useState } from "react";
import { apiGet } from "@/lib/api";

export const UserRole = { Admin: 1, Farmer: 2, Customer: 3, Courier: 4 } as const;
export const FarmerVerificationStatus = { Pending: 1, Verified: 2, Rejected: 3 } as const;
export const ListingStatus = { Draft: 1, Active: 2, OutOfStock: 3, Archived: 4 } as const;
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

export interface AdminOrderDto {
  id: number;
  orderNumber: string;
  customerId: number;
  farmerId: number;
  status: number;
  region: string;
  district: string;
  totalAmount: number;
  createdAt: string;
}

export interface AdminProductListingDto {
  id: number;
  farmerProfileId: number;
  title: string;
  retailPricePerKg: number;
  availableQuantity: number;
  status: number;
  region: string;
  district: string;
  createdAt: string;
}

interface AdminProductsPage {
  items: AdminProductListingDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminFarmerDto {
  id: number;
  userId: number;
  farmName: string;
  region: string;
  district: string;
  verificationStatus: number;
  createdAt: string;
}

export interface AdminUserDto {
  id: number;
  fullName: string;
  email: string;
  phoneNumber: string;
  role: number;
  isActive: boolean;
  createdAt: string;
}

export interface AdminCommissionDto {
  id: number;
  categoryId: number | null;
  percentage: number;
  effectiveFrom: string;
  effectiveTo: string | null;
  createdAt: string;
}

export interface AdminReviewDto {
  id: number;
  orderId: number;
  customerId: number;
  farmerId: number;
  rating: number;
  comment: string | null;
  createdAt: string;
}

export interface AdminSettingDto {
  id: number;
  key: string;
  value: string;
  description: string | null;
  updatedAt: string;
}

export interface TopSellingProductDto {
  productName: string;
  quantitySold: number;
  revenue: number;
}

export interface OrderStatusCountDto {
  status: number;
  count: number;
}

export interface MonthlyRevenueDto {
  year: number;
  month: number;
  revenue: number;
}

export interface AdminAnalyticsDto {
  totalUsers: number;
  totalFarmers: number;
  totalCustomers: number;
  totalCouriers: number;
  totalOrders: number;
  ordersToday: number;
  ordersThisMonth: number;
  totalRevenue: number;
  revenueThisMonth: number;
  totalProductListings: number;
  activeProductListings: number;
  topSellingProducts: TopSellingProductDto[];
  ordersByStatus: OrderStatusCountDto[];
  revenueByMonth: MonthlyRevenueDto[];
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

export function useAdminOrders() {
  const { data, loading, error } = useAsync(() => apiGet<AdminOrderDto[]>("/orders"), []);
  const orders = data ? [...data].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()) : null;
  return { orders, loading, error };
}

// /api/product-listings — единственный список из этих семи с настоящей
// серверной пагинацией (раздел 19 ТЗ) — передаём page напрямую, а не грузим
// всё и режем на фронте, как для остальных.
export function useAdminProducts(page: number, pageSize: number) {
  return useAsync(() => apiGet<AdminProductsPage>(`/product-listings?pageNumber=${page}&pageSize=${pageSize}`), [page, pageSize]);
}

export function useAdminFarmers() {
  const { data, loading, error } = useAsync(() => apiGet<AdminFarmerDto[]>("/farmer-profiles"), []);
  return { farmers: data, loading, error };
}

export function useAdminCustomers() {
  const { data, loading, error } = useAsync(() => apiGet<AdminUserDto[]>("/users"), []);
  const customers = data ? data.filter((u) => u.role === UserRole.Customer) : null;
  return { customers, loading, error };
}

export function useAdminCommissions() {
  const { data, loading, error } = useAsync(() => apiGet<AdminCommissionDto[]>("/commissions"), []);
  return { commissions: data, loading, error };
}

export function useAdminReviews() {
  const { data, loading, error } = useAsync(() => apiGet<AdminReviewDto[]>("/reviews"), []);
  return { reviews: data, loading, error };
}

export function useAdminSettings() {
  const { data, loading, error } = useAsync(() => apiGet<AdminSettingDto[]>("/app-settings"), []);
  return { settings: data, loading, error };
}

export function useAdminAnalytics() {
  const { data, loading, error } = useAsync(() => apiGet<AdminAnalyticsDto>("/analytics/admin/dashboard"), []);
  return { analytics: data, loading, error };
}
