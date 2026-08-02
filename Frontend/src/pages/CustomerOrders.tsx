import { useState } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { ShoppingBag, Star, Truck } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { ViewModeToggle, type OrdersViewMode } from "@/components/ui/ViewModeToggle";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { Button } from "@/components/ui/Button";
import { OrderItemsPhotoList } from "@/components/ui/OrderItemsPhotoList";
import { ReviewModal } from "@/components/customer/ReviewModal";
import { DeliveryStatusBadge } from "@/components/delivery/DeliveryStatusBadge";
import { CustomerDeliveryModal } from "@/components/delivery/CustomerDeliveryModal";
import { useDeliveryByOrder } from "@/data/delivery";
import { formatDateTime, formatSomoni } from "@/lib/utils";
import { ORDER_STATUS_CLASSES, ORDER_STATUS_ICONS, ORDER_STATUS_KEYS, OrderStatus, resolveReceivedAt } from "@/lib/orderStatus";
import {
  submitCustomerReview,
  useCustomerOrders,
  useCustomerProfile,
  useCustomerReviewedOrderIds,
  type CustomerOrderDto,
} from "@/data/customer";
// Delivery и позиции заказа привязаны к заказу через OrderId, не через
// customerId — те же generic-хуки, что уже используются в FarmerOrders.tsx,
// просто ещё один потребитель тех же общих списков.
import { useDeliveriesByOrder, useOrderItems } from "@/data/farmer";
import { useProducts } from "@/data/products";
import { useOrderSequenceMap } from "@/data/orderSequence";

// Кратно 3 — карточный вид на десктопе всегда 3 колонки (см. xl:grid-cols-3
// ниже), так последняя строка страницы не остаётся неполной/одинокой.
const PAGE_SIZE = 9;

// Тот же цветной акцент карточки заказа по статусу, что и в админке
// (см. AdminOrders.tsx) — левая полоса + едва заметная подложка того же
// цветового семейства, что и у бейджа статуса.
const ORDER_STATUS_ACCENT: Record<number, string> = {
  [OrderStatus.Pending]: "border-l-stone-300 bg-stone-50/70 dark:border-l-stone-600 dark:bg-stone-800/40",
  [OrderStatus.Accepted]: "border-l-blue-400 bg-blue-50/70 dark:border-l-blue-600 dark:bg-blue-950/30",
  [OrderStatus.Rejected]: "border-l-rose-400 bg-rose-50/60 dark:border-l-rose-600 dark:bg-rose-950/30",
  [OrderStatus.Preparing]: "border-l-harvest-400 bg-harvest-50/70 dark:border-l-harvest-600 dark:bg-harvest-900/30",
  [OrderStatus.ReadyForPickup]: "border-l-harvest-400 bg-harvest-50/70 dark:border-l-harvest-600 dark:bg-harvest-900/30",
  [OrderStatus.CourierAssigned]: "border-l-blue-400 bg-blue-50/70 dark:border-l-blue-600 dark:bg-blue-950/30",
  [OrderStatus.PickedUp]: "border-l-blue-400 bg-blue-50/70 dark:border-l-blue-600 dark:bg-blue-950/30",
  [OrderStatus.InDelivery]: "border-l-blue-400 bg-blue-50/70 dark:border-l-blue-600 dark:bg-blue-950/30",
  [OrderStatus.Delivered]: "border-l-grove-400 bg-grove-50/70 dark:border-l-grove-600 dark:bg-grove-950/30",
  [OrderStatus.Completed]: "border-l-grove-400 bg-grove-50/70 dark:border-l-grove-600 dark:bg-grove-950/30",
  [OrderStatus.Cancelled]: "border-l-rose-400 bg-rose-50/60 dark:border-l-rose-600 dark:bg-rose-950/30",
};

export function CustomerOrders() {
  const { t } = useTranslation("customer");
  const [page, setPage] = useState(1);
  const [viewMode, setViewMode] = useState<OrdersViewMode>("table");
  const [reviewRefreshKey, setReviewRefreshKey] = useState(0);
  const [reviewingOrder, setReviewingOrder] = useState<CustomerOrderDto | null>(null);
  const [deliveryOrder, setDeliveryOrder] = useState<CustomerOrderDto | null>(null);
  const { profile, loading: profileLoading, error: profileError } = useCustomerProfile();
  const { delivery: viewedDelivery } = useDeliveryByOrder(deliveryOrder?.id ?? null);
  const { orders, loading: ordersLoading, error: ordersError } = useCustomerOrders(profile?.id ?? null);
  const { reviewedOrderIds } = useCustomerReviewedOrderIds(profile?.id ?? null, reviewRefreshKey);
  const { deliveriesByOrderId, loading: deliveriesLoading } = useDeliveriesByOrder();
  const { orderItems } = useOrderItems();
  const products = useProducts();
  const photoByListingId = new Map(products.map((p) => [p.id, p.photoUrl]));
  const sequenceById = useOrderSequenceMap();

  if (profileLoading || (profile && (ordersLoading || deliveriesLoading))) return <PageLoader />;

  if (profileError || ordersError || !profile || !orders) {
    return (
      <EmptyState
        icon={<ShoppingBag size={26} />}
        title={t("orders.errorTitle")}
        description={profileError ?? ordersError ?? t("orders.errorDescription")}
      />
    );
  }

  if (orders.length === 0) {
    return (
      <EmptyState
        icon={<ShoppingBag size={26} />}
        title={t("orders.emptyTitle")}
        description={t("orders.emptyDescription")}
        action={
          <Link to="/catalog" className="inline-flex items-center gap-2 rounded-xl bg-grove-700 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-grove-800">
            {t("dashboard.goToCatalog")}
          </Link>
        }
      />
    );
  }

  const totalPages = Math.max(1, Math.ceil(orders.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: CustomerOrderDto[] = orders.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const handleReviewSubmit = async (rating: number, comment: string) => {
    if (!reviewingOrder) return;
    try {
      await submitCustomerReview(profile.id, {
        orderId: reviewingOrder.id,
        farmerId: reviewingOrder.farmerId,
        rating,
        comment: comment || null,
      });
      toast.success(t("orders.reviewSuccess"));
      setReviewRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("orders.reviewError"), { description: err instanceof Error ? err.message : undefined });
      throw err;
    }
  };

  const statusBadge = (order: CustomerOrderDto) => (
    <StatusBadge
      label={t(`orders.status.${ORDER_STATUS_KEYS[order.status] ?? "pending"}`)}
      className={ORDER_STATUS_CLASSES[order.status] ?? ORDER_STATUS_CLASSES[OrderStatus.Pending]}
      icon={ORDER_STATUS_ICONS[order.status]}
    />
  );

  const reviewAction = (order: CustomerOrderDto) => {
    const alreadyReviewed = reviewedOrderIds.has(order.id);
    if (order.status !== OrderStatus.Completed) return <span className="text-stone-300 dark:text-stone-600">—</span>;
    if (alreadyReviewed) {
      return (
        <span className="inline-flex items-center gap-1.5 text-xs font-semibold text-grove-700 dark:text-grove-400">
          <Star size={13} fill="currentColor" />
          {t("orders.alreadyReviewed")}
        </span>
      );
    }
    return (
      <Button size="sm" variant="outline" leftIcon={<Star size={13} />} onClick={() => setReviewingOrder(order)}>
        {t("orders.writeReview")}
      </Button>
    );
  };

  const deliveryButton = (order: CustomerOrderDto) => {
    const delivery = deliveriesByOrderId.get(order.id);
    return (
      <button
        type="button"
        onClick={() => setDeliveryOrder(order)}
        className="flex flex-col items-start gap-1.5"
      >
        <DeliveryStatusBadge status={delivery?.status ?? null} />
        <span className="flex items-center gap-1 text-xs font-medium text-grove-700 hover:underline dark:text-grove-400">
          <Truck size={11} /> {t("orders.delivery.detailsAction")}
        </span>
      </button>
    );
  };

  const renderCard = (order: CustomerOrderDto) => {
    const receivedAt = resolveReceivedAt(order.status, order.completedAt, deliveriesByOrderId.get(order.id)?.deliveredAt);
    const items = orderItems?.filter((i) => i.orderId === order.id) ?? [];
    return (
      <div
        key={order.id}
        className={`flex flex-col gap-3 rounded-2xl border border-l-4 border-stone-100 p-5 shadow-sm transition hover:shadow-md dark:border-stone-800 ${ORDER_STATUS_ACCENT[order.status] ?? ""}`}
      >
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-3">
            <span
              className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-white font-display text-sm font-semibold text-stone-800 shadow-sm dark:bg-stone-900 dark:text-stone-100"
              title={order.orderNumber}
            >
              {sequenceById.get(order.id) ?? "—"}
            </span>
            <p className="text-xs text-stone-400 dark:text-stone-500">{formatDateTime(order.createdAt)}</p>
          </div>
          <p className="font-display text-lg font-semibold text-grove-700 dark:text-grove-400">
            {formatSomoni(order.totalAmount)} {t("common.somoni")}
          </p>
        </div>
        <div className="border-t border-stone-900/5 pt-3 dark:border-stone-100/5">
          <OrderItemsPhotoList items={items} photoByListingId={photoByListingId} />
        </div>
        <div className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1.5 border-t border-stone-900/5 pt-3 text-sm dark:border-stone-100/5">
          <span className="text-stone-400 dark:text-stone-500">{t("orders.columns.address")}</span>
          <span className="text-stone-700 dark:text-stone-200">
            {order.region}, {order.district}
          </span>
          <span className="text-stone-400 dark:text-stone-500">{t("orders.columns.receivedAt")}</span>
          <span className="text-stone-700 dark:text-stone-200">
            {receivedAt ? formatDateTime(receivedAt) : t("orders.notReceivedYet")}
          </span>
        </div>
        <div className="flex flex-wrap items-center justify-between gap-3 pt-1">
          {statusBadge(order)}
          {reviewAction(order)}
        </div>
        <div className="border-t border-stone-900/5 pt-3 dark:border-stone-100/5">{deliveryButton(order)}</div>
      </div>
    );
  };

  return (
    <div className="overflow-hidden rounded-3xl border border-stone-100 bg-linear-to-b from-white to-stone-50/60 shadow-(--shadow-soft) dark:border-stone-800 dark:from-stone-900 dark:to-stone-900">
      <div className="hidden items-center justify-end border-b border-stone-100 p-4 lg:flex dark:border-stone-800">
        <ViewModeToggle value={viewMode} onChange={setViewMode} ns="customer" />
      </div>

      {/* Десктоп/планшет — обычная таблица, компактно сжатая до 5 колонок,
          чтобы не требовался горизонтальный скролл на типичном ноутбучном
          экране (даже с учётом сайдбара ~256px). */}
      <div className={viewMode === "table" ? "hidden overflow-x-auto lg:block" : "hidden"}>
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
              <th className="px-6 py-4 font-medium">{t("orders.columns.orderNumber")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.address")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.amount")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.status")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.delivery")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.review")}</th>
            </tr>
          </thead>
          <tbody>
            {pageItems.map((order) => {
              const receivedAt = resolveReceivedAt(order.status, order.completedAt, deliveriesByOrderId.get(order.id)?.deliveredAt);
              return (
                <tr key={order.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                  <td className="px-6 py-4">
                    <span className="font-medium text-stone-800 dark:text-stone-100" title={order.orderNumber}>
                      {sequenceById.get(order.id) ?? "—"}
                    </span>
                    <span className="mt-0.5 block text-xs text-stone-400 dark:text-stone-500">{formatDateTime(order.createdAt)}</span>
                  </td>
                  <td className="max-w-56 truncate px-6 py-4 text-stone-500 dark:text-stone-400">
                    {order.region}, {order.district}
                  </td>
                  <td className="px-6 py-4 font-semibold text-stone-800 dark:text-stone-100">
                    {formatSomoni(order.totalAmount)} {t("common.somoni")}
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex flex-col items-start gap-1.5">
                      {statusBadge(order)}
                      <span className="text-xs text-stone-400 dark:text-stone-500">
                        {receivedAt ? formatDateTime(receivedAt) : t("orders.notReceivedYet")}
                      </span>
                    </div>
                  </td>
                  <td className="px-6 py-4">{deliveryButton(order)}</td>
                  <td className="px-6 py-4">{reviewAction(order)}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Десктоп — карточки вместо таблицы, если выбрано в переключателе выше. */}
      {viewMode === "cards" && (
        <div className="hidden grid-cols-2 gap-4 p-5 lg:grid xl:grid-cols-3">{pageItems.map(renderCard)}</div>
      )}

      {/* Мобильный/узкий экран — карточки вместо таблицы всегда, с фото товаров. */}
      <div className="grid grid-cols-1 gap-4 p-5 lg:hidden">{pageItems.map(renderCard)}</div>

      {totalPages > 1 && (
        <div className="border-t border-stone-100 p-4 dark:border-stone-800">
          <Pagination page={currentPage} totalPages={totalPages} onPageChange={setPage} />
        </div>
      )}

      <ReviewModal
        open={reviewingOrder !== null}
        onClose={() => setReviewingOrder(null)}
        orderNumber={reviewingOrder?.orderNumber ?? ""}
        onSubmit={handleReviewSubmit}
      />

      <CustomerDeliveryModal
        open={deliveryOrder !== null}
        onClose={() => setDeliveryOrder(null)}
        delivery={viewedDelivery}
        orderConfirmed={!!deliveryOrder && deliveryOrder.status !== OrderStatus.Pending && deliveryOrder.status !== OrderStatus.Rejected}
        deliveryAddress={deliveryOrder ? `${deliveryOrder.region}, ${deliveryOrder.district}` : ""}
      />
    </div>
  );
}
