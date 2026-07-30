import { useEffect, useMemo, useState } from "react";
import { apiDelete, apiGet, apiPatch, apiPost, apiPut } from "@/lib/api";

export const UserRole = { Admin: 1, Farmer: 2, Customer: 3, Courier: 4 } as const;
export const FarmerVerificationStatus = { Pending: 1, Verified: 2, Rejected: 3 } as const;
export const FarmerDocumentType = { Passport: 1, LandDeed: 2, Other: 3 } as const;
export const DocumentReviewStatus = { Pending: 1, Approved: 2, Rejected: 3 } as const;
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
  // Резолвится на бэкенде через CustomerProfile.UserId → User (см.
  // OrderService.ResolveCustomerContactsAsync) — null, если почему-то нет
  // соответствующей записи (например, удалённый пользователь).
  customerFullName: string | null;
  customerPhone: string | null;
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

export interface AdminProductListingDto {
  id: number;
  farmerProfileId: number;
  title: string;
  unit: string;
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
  village: string;
  address: string;
  description: string | null;
  verificationStatus: number;
  verifiedAt: string | null;
  verifiedByAdminId: number | null;
  createdAt: string;
}

export interface AdminCategoryDto {
  id: number;
  name: string;
  nameTj: string | null;
  nameEn: string | null;
  description: string | null;
  imageUrl: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface AdminCatalogProductDto {
  id: number;
  categoryId: number;
  name: string;
  description: string | null;
  unit: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
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

export interface AdminCourierDto {
  id: number;
  userId: number;
  transportType: string;
  vehicleNumber: string;
  region: string;
  district: string;
  isAvailable: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface AdminDeliveryZoneDto {
  id: number;
  region: string;
  district: string;
  basePrice: number;
  pricePerKm: number | null;
  isActive: boolean;
  createdAt: string;
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

export function useAdminOrders(refreshKey = 0) {
  const { data, loading, error } = useAsync(() => apiGet<AdminOrderDto[]>("/orders"), [refreshKey]);
  // useMemo — критично, не косметика: без него [...data].sort(...) создаёт
  // новый массив на КАЖДЫЙ рендер, даже если data не изменилась. Компоненты
  // (AdminOrders.tsx) кладут orders в зависимости useEffect — с нестабильной
  // ссылкой это давало бесконечный цикл ре-рендеров (markAdminOrdersSeen →
  // notify listeners → forceRerender в AdminLayout → AdminOrders
  // перерисовывается → новый orders → эффект снова срабатывает → ...),
  // из-за которого зависала вся навигация после захода на "Заказы".
  const orders = useMemo(
    () => (data ? [...data].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()) : null),
    [data],
  );
  return { orders, loading, error };
}

// Бейдж "Заказы" у админа — это "новые заказы с последнего просмотра
// списка", а не общий счётчик всех заказов в базе (иначе цифра только
// росла бы и никогда не отражала бы, что уже посмотрели). Так как заказ у
// админа не привязан к какому-то одному ответственному пользователю (в
// отличие от Notification.UserId), "просмотрено" — понятие чисто
// фронтовое: запоминаем максимальный увиденный Order.Id в localStorage.
const ADMIN_ORDERS_SEEN_KEY = "market-tj-admin-orders-last-seen-id";

function readLastSeenAdminOrderId(): number {
  try {
    return Number(localStorage.getItem(ADMIN_ORDERS_SEEN_KEY)) || 0;
  } catch {
    return 0;
  }
}

const adminOrdersSeenListeners = new Set<() => void>();

// Вызывается со страницы "Заказы" после загрузки списка — всё, что там
// сейчас видно, считается просмотренным. AdminLayout (бейдж в сайдбаре) и
// AdminOrders (сама страница) — два разных компонента с двумя независимыми
// вызовами хуков, поэтому бейдж пересчитывается через тот же
// pub/sub-приём, что и notifyFarmerOrdersChanged/notifyNotificationsChanged.
export function markAdminOrdersSeen(orders: AdminOrderDto[]) {
  if (orders.length === 0) return;
  const maxId = Math.max(...orders.map((o) => o.id));
  try {
    localStorage.setItem(ADMIN_ORDERS_SEEN_KEY, String(maxId));
  } catch {
    // localStorage недоступен (приватный режим и т.п.) — бейдж просто не
    // сбросится, не критично для остального функционала.
  }
  adminOrdersSeenListeners.forEach((listener) => listener());
}

export function useAdminOrdersUnseenCount(orders: AdminOrderDto[] | null) {
  const [, forceRerender] = useState(0);

  useEffect(() => {
    const listener = () => forceRerender((k) => k + 1);
    adminOrdersSeenListeners.add(listener);
    return () => {
      adminOrdersSeenListeners.delete(listener);
    };
  }, []);

  if (!orders) return 0;
  const lastSeenId = readLastSeenAdminOrderId();
  return orders.filter((o) => o.id > lastSeenId).length;
}

export interface AdminOrderItemDto {
  id: number;
  orderId: number;
  productListingId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
  createdAt: string;
}

export function useAdminOrderItems() {
  const { data, loading, error } = useAsync(() => apiGet<AdminOrderItemDto[]>("/order-items"), []);
  return { orderItems: data, loading, error };
}

// /api/product-listings — единственный список из этих семи с настоящей
// серверной пагинацией (раздел 19 ТЗ) — передаём page напрямую, а не грузим
// всё и режем на фронте, как для остальных.
export function useAdminProducts(page: number, pageSize: number, refreshKey = 0) {
  return useAsync(() => apiGet<AdminProductsPage>(`/product-listings?pageNumber=${page}&pageSize=${pageSize}`), [page, pageSize, refreshKey]);
}

export function useAdminFarmers(refreshKey = 0) {
  const { data, loading, error } = useAsync(() => apiGet<AdminFarmerDto[]>("/farmer-profiles"), [refreshKey]);
  return { farmers: data, loading, error };
}

// Справочник категорий и базовых товаров (/api/categories, /api/products) —
// из него фермер выбирает при создании объявления (см. useProductCatalog в
// data/farmer.ts). Управляется только Admin.
export function useAdminCategories(refreshKey = 0) {
  const { data, loading, error } = useAsync(() => apiGet<AdminCategoryDto[]>("/categories"), [refreshKey]);
  return { categories: data, loading, error };
}

export function useAdminCatalogProducts(refreshKey = 0) {
  const { data, loading, error } = useAsync(() => apiGet<AdminCatalogProductDto[]>("/products"), [refreshKey]);
  return { products: data, loading, error };
}

export function useAdminCustomers() {
  const { data, loading, error } = useAsync(() => apiGet<AdminUserDto[]>("/users"), []);
  const customers = useMemo(() => (data ? data.filter((u) => u.role === UserRole.Customer) : null), [data]);
  return { customers, loading, error };
}

export function useAdminCommissions(refreshKey = 0) {
  const { data, loading, error } = useAsync(() => apiGet<AdminCommissionDto[]>("/commissions"), [refreshKey]);
  return { commissions: data, loading, error };
}

export function useAdminReviews(refreshKey = 0) {
  const { data, loading, error } = useAsync(() => apiGet<AdminReviewDto[]>("/reviews"), [refreshKey]);
  return { reviews: data, loading, error };
}

export function useAdminSettings(refreshKey = 0) {
  const { data, loading, error } = useAsync(() => apiGet<AdminSettingDto[]>("/app-settings"), [refreshKey]);
  return { settings: data, loading, error };
}

// Структурированные "общие настройки платформы" (Admin → Настройки) — читаются
// и пишутся одним запросом (GET/PUT /api/admin/settings), в отличие от
// generic key-value CRUD выше (/app-settings), который остаётся для
// произвольных ad-hoc настроек.
export interface PlatformSettingsDto {
  siteName: string;
  logoUrl: string | null;
  contactEmail: string;
  contactPhone: string;
  commissionPercent: number;
  currency: string;
  minimumOrderAmount: number;
  maintenanceModeEnabled: boolean;
  maintenanceMessage: string | null;
  emailNotificationsEnabled: boolean;
  smsNotificationsEnabled: boolean;
  updatedAt: string | null;
  updatedByAdminId: number | null;
}

export type PlatformSettingsFormDto = Omit<PlatformSettingsDto, "updatedAt" | "updatedByAdminId">;

export function usePlatformSettings(refreshKey = 0) {
  const { data, loading, error } = useAsync(() => apiGet<PlatformSettingsDto>("/admin/settings"), [refreshKey]);
  return { settings: data, loading, error };
}

export function updatePlatformSettings(dto: PlatformSettingsFormDto) {
  return apiPut<string>("/admin/settings", dto);
}

// Модерация документов фермеров (паспорт и т.д.) — раньше документ загружался
// фермером, но нигде не был виден админу для одобрения/отклонения.
export interface AdminFarmerDocumentDto {
  id: number;
  farmerProfileId: number;
  farmName: string;
  farmerFullName: string | null;
  documentType: number;
  fileUrl: string;
  status: number;
  uploadedAt: string;
  reviewedAt: string | null;
  reviewedByAdminId: number | null;
  rejectionReason: string | null;
}

interface AdminFarmerDocumentsPage {
  items: AdminFarmerDocumentDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export function useAdminFarmerDocuments(page: number, pageSize: number, status: number | null, refreshKey = 0) {
  const statusQuery = status !== null ? `&status=${status}` : "";
  return useAsync(
    () => apiGet<AdminFarmerDocumentsPage>(`/admin/farmer-documents?pageNumber=${page}&pageSize=${pageSize}${statusQuery}`),
    [page, pageSize, status, refreshKey],
  );
}

export function reviewFarmerDocument(id: number, status: number, rejectionReason: string | null) {
  return apiPatch<string>(`/admin/farmer-documents/${id}/review`, { status, rejectionReason });
}

export function useAdminCouriers(refreshKey = 0) {
  const { data, loading, error } = useAsync(() => apiGet<AdminCourierDto[]>("/courier-profiles"), [refreshKey]);
  return { couriers: data, loading, error };
}

export function useAdminDeliveryZones(refreshKey = 0) {
  const { data, loading, error } = useAsync(() => apiGet<AdminDeliveryZoneDto[]>("/delivery-zones"), [refreshKey]);
  return { zones: data, loading, error };
}

export function useAdminAnalytics() {
  const { data, loading, error } = useAsync(() => apiGet<AdminAnalyticsDto>("/analytics/admin/dashboard"), []);
  return { analytics: data, loading, error };
}

// --- Мутации (раздел 19 ТЗ — стандартный CRUD, уже готовый на бэкенде) ---

// UpdateFarmerProfileDto требует все поля профиля, а не только те, что
// меняются — реконструируем DTO из уже загруженной записи, меняя только
// verificationStatus/verifiedAt/verifiedByAdminId.
export function updateFarmerVerification(farmer: AdminFarmerDto, verificationStatus: number, adminUserId: number) {
  return apiPut<string>(`/farmer-profiles/${farmer.id}`, {
    id: farmer.id,
    userId: farmer.userId,
    farmName: farmer.farmName,
    region: farmer.region,
    district: farmer.district,
    village: farmer.village,
    address: farmer.address,
    description: farmer.description,
    verificationStatus,
    verifiedAt: new Date().toISOString(),
    verifiedByAdminId: adminUserId,
  });
}

// Дополнено по явному запросу пользователя — раньше документ можно было
// загрузить (см. FarmerDocuments.tsx), но проверить его в админке было
// негде, хотя backend (UpdateAsync) уже полностью это поддерживал.
export interface CommissionFormDto {
  categoryId: number | null;
  percentage: number;
  effectiveFrom: string;
  effectiveTo: string | null;
}

export function createCommission(dto: CommissionFormDto) {
  return apiPost<string>("/commissions", dto);
}

export function updateCommission(id: number, dto: CommissionFormDto) {
  return apiPut<string>(`/commissions/${id}`, { id, ...dto });
}

export function deleteCommission(id: number) {
  return apiDelete<string>(`/commissions/${id}`);
}

export function updateSettingValue(setting: AdminSettingDto, value: string, adminUserId: number) {
  return apiPut<string>(`/app-settings/${setting.id}`, {
    id: setting.id,
    key: setting.key,
    value,
    description: setting.description,
    updatedByAdminId: adminUserId,
  });
}

export interface AppSettingFormDto {
  key: string;
  value: string;
  description: string | null;
}

export function createAppSetting(dto: AppSettingFormDto, adminUserId: number) {
  return apiPost<string>("/app-settings", { ...dto, updatedByAdminId: adminUserId });
}

export function deleteAppSetting(id: number) {
  return apiDelete<string>(`/app-settings/${id}`);
}

// Бэкенд сам расставляет AcceptedAt/CompletedAt/CancelledAt при первом
// переходе в соответствующий статус (см. OrderService.UpdateAsync) — просто
// передаём остальные поля заказа без изменений и новый статус.
export function updateOrderStatus(order: AdminOrderDto, status: number) {
  return apiPut<string>(`/orders/${order.id}`, {
    id: order.id,
    orderNumber: order.orderNumber,
    customerId: order.customerId,
    farmerId: order.farmerId,
    status,
    deliveryAddress: order.deliveryAddress,
    region: order.region,
    district: order.district,
    customerComment: order.customerComment,
    subtotal: order.subtotal,
    deliveryPrice: order.deliveryPrice,
    totalAmount: order.totalAmount,
    acceptedAt: order.acceptedAt,
    completedAt: order.completedAt,
    cancelledAt: order.cancelledAt,
  });
}

// Notification.UserId — это User.Id, а Order.CustomerId — CustomerProfile.Id
// (та же нестыковка, что и у Farmer, см. комментарий в data/customer.ts к
// submitCustomerOrder) — резолвим через /customer-profiles. Уведомление не
// часть самого обновления статуса, поэтому ошибку глушим, не пробрасываем.
export async function notifyCustomerAboutOrderStatus(order: AdminOrderDto, status: number) {
  if (status !== OrderStatus.Delivered && status !== OrderStatus.Completed) return;

  try {
    const profiles = await apiGet<{ id: number; userId: number }[]>("/customer-profiles");
    const customerUserId = profiles.find((p) => p.id === order.customerId)?.userId;
    if (!customerUserId) return;

    const { title, message } =
      status === OrderStatus.Completed
        ? {
            title: "Заказ завершён",
            message: `Вы получили заказ ${order.orderNumber}. Спасибо за покупку! Будем рады, если оцените заказ.`,
          }
        : {
            title: "Заказ доставлен",
            message: `Ваш заказ ${order.orderNumber} доставлен.`,
          };

    await apiPost<string>("/notifications", { userId: customerUserId, title, message, isRead: false });
  } catch (err) {
    console.error("Не удалось отправить уведомление покупателю о статусе заказа", err);
  }
}

export function deleteReview(id: number) {
  return apiDelete<string>(`/reviews/${id}`);
}

export function deleteAdminProductListing(id: number) {
  return apiDelete<string>(`/product-listings/${id}`);
}

// UpdateCourierProfileDto требует полный набор полей — переносим текущие
// значения и меняем только isActive/isAvailable (быстрые действия в таблице).
export function updateCourierStatus(courier: AdminCourierDto, changes: { isActive?: boolean; isAvailable?: boolean }) {
  return apiPut<string>(`/courier-profiles/${courier.id}`, {
    id: courier.id,
    userId: courier.userId,
    transportType: courier.transportType,
    vehicleNumber: courier.vehicleNumber,
    region: courier.region,
    district: courier.district,
    isAvailable: changes.isAvailable ?? courier.isAvailable,
    isActive: changes.isActive ?? courier.isActive,
  });
}

export interface DeliveryZoneFormDto {
  region: string;
  district: string;
  basePrice: number;
  pricePerKm: number | null;
  isActive: boolean;
}

export function createDeliveryZone(dto: DeliveryZoneFormDto) {
  return apiPost<string>("/delivery-zones", dto);
}

export function updateDeliveryZone(id: number, dto: DeliveryZoneFormDto) {
  return apiPut<string>(`/delivery-zones/${id}`, { id, ...dto });
}

export function deleteDeliveryZone(id: number) {
  return apiDelete<string>(`/delivery-zones/${id}`);
}

export interface CategoryFormDto {
  name: string;
  nameTj: string;
  nameEn: string;
  description: string | null;
  imageUrl: string | null;
  isActive: boolean;
}

export function createCategory(dto: CategoryFormDto) {
  return apiPost<string>("/categories", dto);
}

export function updateCategory(id: number, dto: CategoryFormDto) {
  return apiPut<string>(`/categories/${id}`, { id, ...dto });
}

export function deleteCategory(id: number) {
  return apiDelete<string>(`/categories/${id}`);
}

export interface CatalogProductFormDto {
  categoryId: number;
  name: string;
  description: string | null;
  unit: string;
  isActive: boolean;
}

export function createCatalogProduct(dto: CatalogProductFormDto) {
  return apiPost<string>("/products", dto);
}

export function updateCatalogProduct(id: number, dto: CatalogProductFormDto) {
  return apiPut<string>(`/products/${id}`, { id, ...dto });
}

export function deleteCatalogProduct(id: number) {
  return apiDelete<string>(`/products/${id}`);
}

// Своя учётка админа — отдельного AdminProfile-справочника в проекте нет
// (в отличие от Farmer/Customer), поэтому переиспользуем существующий
// Admin-only GET/PUT /api/users/{id} (UserController) — сам на себя тоже
// имеет право, ограничения по "чужой/свой" там нет.
export interface AdminOwnUserDto {
  id: number;
  fullName: string;
  email: string;
  phoneNumber: string;
  avatarUrl: string | null;
  role: number;
  isActive: boolean;
  createdAt: string;
}

export function useAdminOwnUser(userId: number | null, refreshKey = 0) {
  const { data, loading, error } = useAsync(
    () => (userId ? apiGet<AdminOwnUserDto>(`/users/${userId}`) : Promise.resolve(null as never)),
    [userId, refreshKey],
  );
  return { account: data, loading, error };
}

export interface AdminAccountFormDto {
  fullName: string;
  email: string;
  phoneNumber: string;
}

export function updateAdminOwnAccount(current: AdminOwnUserDto, edits: AdminAccountFormDto) {
  return apiPut<string>(`/users/${current.id}`, {
    id: current.id,
    fullName: edits.fullName,
    email: edits.email,
    phoneNumber: edits.phoneNumber,
    password: null,
    role: current.role,
    isActive: current.isActive,
  });
}
