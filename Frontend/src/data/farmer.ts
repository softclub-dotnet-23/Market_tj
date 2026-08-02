import { useEffect, useMemo, useState } from "react";
import { apiDelete, apiGet, apiPatch, apiPost, apiPut, apiUpload, resolveMediaUrl } from "@/lib/api";
import { useAuth } from "@/context/AuthContext";
import { productPhotos } from "@/assets/photos";
import { FALLBACK_PHOTO_ID_BY_PRODUCT_NAME } from "@/data/productPhotoFallback";

export const ListingStatus = { Draft: 1, Active: 2, OutOfStock: 3, Archived: 4 } as const;
export const FarmerVerificationStatus = { Pending: 1, Verified: 2, Rejected: 3 } as const;
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
export const DeliveryStatus = { Pending: 1, Assigned: 2, PickedUp: 3, InDelivery: 4, Delivered: 5, Cancelled: 6 } as const;
export const FarmerDocumentType = { Passport: 1, LandDeed: 2, Other: 3, PassportFront: 4, PassportBack: 5, Selfie: 6 } as const;
export const DocumentReviewStatus = { Pending: 1, Approved: 2, Rejected: 3 } as const;

// Дополнено по явному запросу пользователя (2026-08-02) — фермер не может
// добавить товар, пока не отправил паспорт (обе стороны) и селфи. Rejected
// не считается "отправленным" — тот же критерий, что и на бэкенде
// (ProductListingService.HasRequiredDocumentsAsync).
export const REQUIRED_FARMER_DOCUMENT_TYPES = [
  FarmerDocumentType.PassportFront,
  FarmerDocumentType.PassportBack,
  FarmerDocumentType.Selfie,
] as const;

export function hasRequiredFarmerDocuments(documents: FarmerDocumentDto[] | null): boolean {
  if (!documents) return false;
  const submittedTypes = new Set(documents.filter((d) => d.status !== DocumentReviewStatus.Rejected).map((d) => d.documentType));
  return REQUIRED_FARMER_DOCUMENT_TYPES.every((type) => submittedTypes.has(type));
}

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
  autoReplyToReviewsEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

// Переключатель "отвечать на отзывы автоматически через AI" — по прямому
// запросу пользователя (2026-08-02): включено — новый отзыв сразу получает
// ответ от AI без участия фермера (см. ReviewService.CreateAsync на бэкенде);
// выключено (по умолчанию) — фермер отвечает вручную (см. replyToReview выше).
export function setFarmerAutoReply(farmerProfileId: number, enabled: boolean) {
  return apiPatch<string>(`/farmer-profiles/${farmerProfileId}/auto-reply`, { enabled });
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
  categoryId: number;
  unit: string;
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

export interface CreateProductListingDto {
  farmerProfileId: number;
  categoryId: number;
  unit: string;
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

export interface FarmerOrderDto {
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

export interface OrderItemDto {
  id: number;
  orderId: number;
  productListingId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
  createdAt: string;
}

// /api/order-items не фильтрует по фермеру на бэкенде — та же схема, что и
// везде в этом файле: грузим всё и сопоставляем по productListingId на фронте.
export function useOrderItems() {
  const { data, loading, error } = useAsync(() => apiGet<OrderItemDto[]>("/order-items"), []);
  return { orderItems: data, loading, error };
}

export interface DeliveryDto {
  id: number;
  orderId: number;
  courierId: number | null;
  status: number;
  assignedAt: string | null;
  pickedUpAt: string | null;
  deliveredAt: string | null;
}

export interface FarmerReviewDto {
  id: number;
  orderId: number;
  customerId: number;
  customerFullName: string | null;
  farmerId: number;
  rating: number;
  comment: string | null;
  createdAt: string;
  farmerReply: string | null;
  farmerRepliedAt: string | null;
}

// По прямому запросу пользователя (2026-08-02) — фермер отвечает на отзыв о
// себе вручную или текстом, который предложил AI-ассистент (см.
// propose_reply_review в AiAssistantService, тот же PATCH-эндпоинт).
export function replyToReview(reviewId: number, reply: string) {
  return apiPatch<string>(`/reviews/${reviewId}/reply`, { reply });
}

export interface NotificationDto {
  id: number;
  userId: number;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface FarmerDocumentDto {
  id: number;
  farmerProfileId: number;
  documentType: number;
  fileUrl: string;
  status: number;
  uploadedAt: string;
  reviewedAt: string | null;
  reviewedByAdminId: number | null;
  rejectionReason: string | null;
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
export function useFarmerProfile(refreshKey = 0) {
  const { user } = useAuth();
  const { data: profiles, loading, error } = useAsync(() => apiGet<FarmerProfileDto[]>("/farmer-profiles"), [refreshKey]);
  const profile = profiles?.find((p) => p.userId === user?.userId) ?? null;
  return { profile, loading, error };
}

export interface FarmerProfileEditableFields {
  farmName: string;
  region: string;
  district: string;
  village: string;
  address: string;
  description: string | null;
}

export interface CreateFarmerProfilePayload {
  userId: number;
  farmName: string;
  region: string;
  district: string;
  village: string;
  address: string;
  description: string | null;
}

// Второй шаг после POST /auth/register (раздел 23 ТЗ, Этап 3) — профиль
// создаётся отдельным вызовом уже с токеном, выданным при регистрации.
// verificationStatus всегда Pending — подтверждает только Admin.
export function createFarmerProfile(payload: CreateFarmerProfilePayload) {
  return apiPost<string>("/farmer-profiles", {
    ...payload,
    verificationStatus: FarmerVerificationStatus.Pending,
    verifiedAt: null,
    verifiedByAdminId: null,
  });
}

// UpdateFarmerProfileDto требует ВСЕ поля профиля, включая verificationStatus/
// verifiedAt/verifiedByAdminId — их нельзя отдавать на откуп фермеру (это
// раздел 10.1 ТЗ, подтверждает только Admin), поэтому переносим их из уже
// загруженной записи без изменений, меняя только редактируемые поля.
export function updateFarmerProfile(current: FarmerProfileDto, edits: FarmerProfileEditableFields) {
  return apiPut<string>(`/farmer-profiles/${current.id}`, {
    id: current.id,
    userId: current.userId,
    farmName: edits.farmName,
    region: edits.region,
    district: edits.district,
    village: edits.village,
    address: edits.address,
    description: edits.description,
    verificationStatus: current.verificationStatus,
    verifiedAt: current.verifiedAt,
    verifiedByAdminId: current.verifiedByAdminId,
  });
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

export function createProductListing(dto: CreateProductListingDto) {
  return apiPost<string>("/product-listings", dto);
}

// UpdateProductListingDto на бэкенде требует Id и в теле запроса, не только в URL.
export function updateProductListing(id: number, dto: CreateProductListingDto) {
  return apiPut<string>(`/product-listings/${id}`, { id, ...dto });
}

export function deleteProductListing(id: number) {
  return apiDelete<string>(`/product-listings/${id}`);
}

export interface ProductImageDto {
  id: number;
  productListingId: number;
  imageUrl: string;
  isMain: boolean;
  createdAt: string;
}

// /api/product-images не фильтрует по объявлению на бэкенде — та же схема,
// что и везде в этом файле: грузим всё и отфильтровываем на фронте.
export function useProductImages(productListingId: number | null, refreshKey = 0) {
  const { data, loading, error } = useAsync(
    () => (productListingId ? apiGet<ProductImageDto[]>("/product-images") : Promise.resolve(null as never)),
    [productListingId, refreshKey],
  );
  const images = data?.filter((i) => i.productListingId === productListingId) ?? null;
  return { images, loading, error };
}

function fallbackPhotoByTitle(title: string): string | undefined {
  const matchedName = Object.keys(FALLBACK_PHOTO_ID_BY_PRODUCT_NAME).find((name) => title.includes(name));
  return matchedName ? productPhotos[FALLBACK_PHOTO_ID_BY_PRODUCT_NAME[matchedName]] : undefined;
}

// Для миниатюр в табличных/карточных списках (Admin/FarmerProducts) — по
// одной главной фотографии на объявление, вне зависимости от статуса/фермера
// (в отличие от публичного каталога, который отдаёт фото только для активных
// объявлений проверенных фермеров, см. catalogStore.ts). "Грузим всё,
// фильтруем на фронте" — тот же приём, что и у useProductImages, просто на
// всех сразу. Если фермер ещё не загрузил настоящее фото объявления — та же
// честная заглушка "похожая на товар картинка" по названию, что и в
// публичном каталоге (см. data/productPhotoFallback.ts), а не пустая иконка:
// у большинства демо-объявлений реальных фото ещё нет.
export function useProductPhotoMap(products: { id: number; title: string }[] | null | undefined): Map<number, string> {
  const { data } = useAsync(() => apiGet<ProductImageDto[]>("/product-images"), []);
  const map = new Map<number, string>();
  (data ?? []).forEach((img) => {
    if (!map.has(img.productListingId) || img.isMain) map.set(img.productListingId, resolveMediaUrl(img.imageUrl));
  });
  (products ?? []).forEach((p) => {
    if (map.has(p.id)) return;
    const fallback = fallbackPhotoByTitle(p.title);
    if (fallback) map.set(p.id, fallback);
  });
  return map;
}

export function uploadProductImage(productListingId: number, isMain: boolean, file: File) {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("productListingId", String(productListingId));
  formData.append("isMain", String(isMain));
  return apiUpload<ProductImageDto>("/product-images/upload", formData);
}

export function deleteProductImage(id: number) {
  return apiDelete<string>(`/product-images/${id}`);
}

// FarmerLayout (бейдж в сайдбаре) и FarmerOrders (сама страница) — два разных
// компонента с двумя независимыми вызовами этого хука, у каждого свой locale
// state. Без этого моста смена статуса на странице заказов не отразилась бы
// на бейдже, пока не перемонтируется весь layout — оповещаем все подписанные
// вызовы напрямую, без похода за общим кэшем (как в catalogStore.ts, тут это
// было бы избыточно ради одного счётчика).
const farmerOrdersListeners = new Set<() => void>();

export function notifyFarmerOrdersChanged() {
  farmerOrdersListeners.forEach((listener) => listener());
}

// /api/orders не фильтрует по фермеру на бэкенде — та же схема, что и с
// профилем/товарами: грузим всё и отфильтровываем по farmerId на фронте.
export function useFarmerOrders(farmerProfileId: number | null, refreshKey = 0) {
  const [externalRefresh, setExternalRefresh] = useState(0);

  useEffect(() => {
    const listener = () => setExternalRefresh((k) => k + 1);
    farmerOrdersListeners.add(listener);
    return () => {
      farmerOrdersListeners.delete(listener);
    };
  }, []);

  const { data, loading, error } = useAsync(
    () => (farmerProfileId ? apiGet<FarmerOrderDto[]>("/orders") : Promise.resolve(null as never)),
    [farmerProfileId, refreshKey, externalRefresh],
  );
  // useMemo — без него filter+sort пересоздавали бы новый массив на каждый
  // рендер даже при неизменной data, что для листов с частыми ре-рендерами
  // (см. AdminOrders.tsx — тот же паттерн там довёл до бесконечного цикла) —
  // лишняя нагрузка, которой стоит избегать системно, а не только там, где
  // уже рвануло.
  const orders = useMemo(
    () =>
      data
        ?.filter((o) => o.farmerId === farmerProfileId)
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()) ?? null,
    [data, farmerProfileId],
  );
  return { orders, loading, error };
}

// Фермер может принять/отклонить и подготовить заказ — дальше (сборка курьером
// и всё, что после) ведёт Admin (см. lib/orderStatus.ts, getFarmerNextStatuses).
// Бэкенд сам расставляет AcceptedAt при первом переходе в Accepted.
export function updateFarmerOrderStatus(order: FarmerOrderDto, status: number) {
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

// Delivery привязан к заказу через OrderId (не через farmerId напрямую) —
// грузим все записи о доставке и сопоставляем с заказами фермера на фронте,
// чтобы показать, назначен ли курьер и на каком этапе доставка.
export function useDeliveriesByOrder() {
  const { data, loading, error } = useAsync(() => apiGet<DeliveryDto[]>("/deliveries"), []);
  const byOrderId = useMemo(() => {
    const map = new Map<number, DeliveryDto>();
    data?.forEach((d) => map.set(d.orderId, d));
    return map;
  }, [data]);
  return { deliveriesByOrderId: byOrderId, loading, error };
}

// Отзывы оставляют покупатели после заказа — фермер только читает их, не
// создаёт и не удаляет (модерация — задача Admin).
export function useFarmerReviews(farmerProfileId: number | null, refreshKey = 0) {
  const { data, loading, error } = useAsync(
    () => (farmerProfileId ? apiGet<FarmerReviewDto[]>("/reviews") : Promise.resolve(null as never)),
    [farmerProfileId, refreshKey],
  );
  const reviews = useMemo(
    () =>
      data
        ?.filter((r) => r.farmerId === farmerProfileId)
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()) ?? null,
    [data, farmerProfileId],
  );
  return { reviews, loading, error };
}

// Бейдж-колокольчик в сайдбаре и сама страница уведомлений — это два разных
// вызова этого хука в разных компонентах (как и с useFarmerOrders выше) —
// без моста между ними прочтение уведомлений на странице не сбросило бы
// цифру в сайдбаре без перезагрузки. notifyNotificationsChanged() вызывается
// после авто-прочтения (см. NotificationCenter) и будит все подписанные
// вызовы хука сразу, без похода за общим кэшем.
const notificationsListeners = new Set<() => void>();

export function notifyNotificationsChanged() {
  notificationsListeners.forEach((listener) => listener());
}

// Notification.UserId — прямой FK на User.Id (без промежуточного профиля),
// поэтому фильтруем по user.userId из JWT напрямую, без похода за списком профилей.
export function useFarmerNotifications(userId: number | null, refreshKey = 0) {
  const [externalRefresh, setExternalRefresh] = useState(0);

  useEffect(() => {
    const listener = () => setExternalRefresh((k) => k + 1);
    notificationsListeners.add(listener);
    return () => {
      notificationsListeners.delete(listener);
    };
  }, []);

  const { data, loading, error } = useAsync(
    () => (userId ? apiGet<NotificationDto[]>("/notifications") : Promise.resolve(null as never)),
    [userId, refreshKey, externalRefresh],
  );
  // useMemo — этот хук вызывается на каждом рендере layout'а (бейдж-
  // колокольчик в AdminLayout/FarmerLayout/CustomerLayout), так что
  // нестабильная ссылка здесь пересчитывалась бы особенно часто.
  const notifications = useMemo(
    () =>
      data
        ?.filter((n) => n.userId === userId)
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()) ?? null,
    [data, userId],
  );
  return { notifications, loading, error };
}

export function markNotificationRead(notification: NotificationDto) {
  return apiPut<string>(`/notifications/${notification.id}`, {
    id: notification.id,
    userId: notification.userId,
    title: notification.title,
    message: notification.message,
    isRead: true,
  });
}

export function useFarmerDocuments(farmerProfileId: number | null, refreshKey = 0) {
  const { data, loading, error } = useAsync(
    () => (farmerProfileId ? apiGet<FarmerDocumentDto[]>("/farmer-documents") : Promise.resolve(null as never)),
    [farmerProfileId, refreshKey],
  );
  const documents = data?.filter((d) => d.farmerProfileId === farmerProfileId) ?? null;
  return { documents, loading, error };
}

// Дополнено по явному запросу пользователя — раньше загрузка нового
// документа не работала вообще (не было файлового эндпоинта на бэкенде,
// см. POST /farmer-documents/upload). Новый документ всегда создаётся со
// статусом "На проверке" — админ должен подтвердить/отклонить его отдельно.
export function uploadFarmerDocument(farmerProfileId: number, documentType: number, file: File) {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("farmerProfileId", String(farmerProfileId));
  formData.append("documentType", String(documentType));
  return apiUpload<FarmerDocumentDto>("/farmer-documents/upload", formData);
}

export function deleteFarmerDocument(id: number) {
  return apiDelete<string>(`/farmer-documents/${id}`);
}

