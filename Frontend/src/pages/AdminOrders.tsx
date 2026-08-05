import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Search, ShoppingCart } from "lucide-react";
import { useAdminSearch } from "@/components/layout/AdminLayout";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { ViewModeToggle, type OrdersViewMode } from "@/components/ui/ViewModeToggle";
import { StatusMenu } from "@/components/ui/StatusMenu";
import { OrderItemsCell } from "@/components/ui/OrderItemsCell";
import { OrderItemsPhotoList } from "@/components/ui/OrderItemsPhotoList";
import { DeliveryStatusBadge } from "@/components/delivery/DeliveryStatusBadge";
import { formatDateTime, formatSomoni } from "@/lib/utils";
import { ORDER_STATUS_CLASSES, ORDER_STATUS_ICONS, ORDER_STATUS_KEYS, getAdminNextStatuses, resolveReceivedAt } from "@/lib/orderStatus";
import {
  OrderStatus,
  markAdminOrdersSeen,
  notifyCustomerAboutOrderStatus,
  updateOrderStatus,
  useAdminFarmers,
  useAdminOrderItems,
  useAdminOrders,
  type AdminOrderDto,
} from "@/data/adminEntities";
// Delivery — общий для всех ролей список (та же схема, что уже используется
// у Farmer/Customer): грузим все записи о доставке и сопоставляем с заказом
// по orderId, чтобы показать реальное время получения.
import { useDeliveriesByOrder } from "@/data/farmer";
import { useProducts } from "@/data/products";
import { useOrderSequenceMap } from "@/data/orderSequence";

// Кратно 3 — карточный вид на десктопе всегда 3 колонки (см. xl:grid-cols-3
// ниже), так последняя строка страницы не остаётся неполной/одинокой.
const PAGE_SIZE = 9;

// Лёгкий цветной акцент карточки заказа по статусу (левая полоса + едва
// заметная подложка) — то же цветовое семейство, что и у бейджа статуса
// (ORDER_STATUS_CLASSES), просто перенесённое на всю карточку, чтобы список
// заказов не выглядел монотонным белым полотном.
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

export function AdminOrders() {
  const { t } = useTranslation("admin");
  const searchQuery = useAdminSearch();
  const [page, setPage] = useState(1);
  const [viewMode, setViewMode] = useState<OrdersViewMode>("table");
  const [refreshKey, setRefreshKey] = useState(0);
  const [busyId, setBusyId] = useState<number | null>(null);
  const { orders, loading, error } = useAdminOrders(refreshKey);
  const { deliveriesByOrderId, loading: deliveriesLoading } = useDeliveriesByOrder();
  const { orderItems } = useAdminOrderItems();
  const { farmers } = useAdminFarmers();
  const products = useProducts();
  const photoByListingId = new Map(products.map((p) => [p.id, p.photoUrl]));
  const farmNameById = new Map((farmers ?? []).map((f) => [f.id, f.farmName]));
  const sequenceById = useOrderSequenceMap();

  // Всё, что видно на этой странице, считается просмотренным — бейдж
  // "Заказы" в сайдбаре (AdminLayout) сразу пересчитывается через
  // markAdminOrdersSeen (см. data/adminEntities.ts), без перезагрузки.
  useEffect(() => {
    if (orders) markAdminOrdersSeen(orders);
  }, [orders]);

  useEffect(() => {
    setPage(1);
  }, [searchQuery]);

  if (loading || deliveriesLoading) return <PageLoader />;

  if (error || !orders) {
    return <EmptyState icon={<ShoppingCart size={26} />} title={t("orders.errorTitle")} description={error ?? t("orders.errorDescription")} />;
  }

  if (orders.length === 0) {
    return <EmptyState icon={<ShoppingCart size={26} />} title={t("orders.emptyTitle")} description={t("orders.emptyDescription")} />;
  }

  const query = searchQuery.trim().toLowerCase();
  const filteredOrders = query
    ? orders.filter((o) => {
        const farmerName = farmNameById.get(o.farmerId) ?? "";
        const sequence = sequenceById.get(o.id);
        return (
          (sequence !== undefined && String(sequence).includes(query)) ||
          o.orderNumber.toLowerCase().includes(query) ||
          (o.customerFullName ?? "").toLowerCase().includes(query) ||
          farmerName.toLowerCase().includes(query) ||
          `${o.region}, ${o.district}`.toLowerCase().includes(query)
        );
      })
    : orders;

  if (query && filteredOrders.length === 0) {
    return <EmptyState icon={<Search size={26} />} title={t("common.searchEmptyTitle")} description={t("common.searchEmptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(filteredOrders.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminOrderDto[] = filteredOrders.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const handleStatusChange = async (order: AdminOrderDto, status: number) => {
    if (status === order.status) return;
    setBusyId(order.id);
    try {
      await updateOrderStatus(order, status);
      toast.success(t("orders.updateSuccess"));
      void notifyCustomerAboutOrderStatus(order, status);
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("orders.updateError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setBusyId(null);
    }
  };

  const participants = (order: AdminOrderDto) => (
    <div className="flex flex-col gap-1">
      <span className="block truncate" title={order.customerFullName ?? undefined}>
        <span className="text-stone-400 dark:text-stone-500">{t("orders.columns.customer")}: </span>
        {order.customerFullName ?? t("orders.customerLabel", { id: order.customerId })}
      </span>
      <span className="block truncate" title={farmNameById.get(order.farmerId)}>
        <span className="text-stone-400 dark:text-stone-500">{t("orders.columns.farmer")}: </span>
        {farmNameById.get(order.farmerId) ?? t("orders.farmerLabel", { id: order.farmerId })}
      </span>
    </div>
  );

  const statusCell = (order: AdminOrderDto, receivedAt: string | null) => (
    <div className="flex flex-col items-start gap-1.5">
      <StatusMenu
        value={order.status}
        busy={busyId === order.id}
        lockedLabel={t("orders.statusLockedHint")}
        onChange={(status) => handleStatusChange(order, status)}
        options={[order.status, ...getAdminNextStatuses(order.status)].map((s) => ({
          value: s,
          label: t(`orders.status.${ORDER_STATUS_KEYS[s]}`),
          className: ORDER_STATUS_CLASSES[s],
          icon: ORDER_STATUS_ICONS[s],
        }))}
      />
      <span className="text-xs text-stone-400 dark:text-stone-500">
        {receivedAt ? formatDateTime(receivedAt) : t("orders.notReceivedYet")}
      </span>
    </div>
  );

  // По прямому запросу пользователя (2026-08-05): назначает курьера только
  // фермер (см. FarmerOrders.tsx/AssignCourierDrawer) — у админа здесь
  // остаётся только просмотр статуса доставки, без действий.
  const deliveryCell = (order: AdminOrderDto) => {
    const delivery = deliveriesByOrderId.get(order.id);
    return (
      <div className="flex flex-col items-start gap-1.5">
        <DeliveryStatusBadge status={delivery?.status ?? null} />
        {(delivery?.manualCourierName ?? delivery?.courierFullName) && (
          <span className="text-xs text-stone-400 dark:text-stone-500">{delivery.manualCourierName ?? delivery.courierFullName}</span>
        )}
      </div>
    );
  };

  const renderCard = (order: AdminOrderDto) => {
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
        <div className="text-sm text-stone-600 dark:text-stone-300">{participants(order)}</div>
        <div className="border-t border-stone-900/5 pt-3 dark:border-stone-100/5">
          <OrderItemsPhotoList items={items} photoByListingId={photoByListingId} />
        </div>
        <div className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1.5 border-t border-stone-900/5 pt-3 text-sm dark:border-stone-100/5">
          <span className="text-stone-400 dark:text-stone-500">{t("orders.columns.region")}</span>
          <span className="text-stone-700 dark:text-stone-200">
            {order.region}, {order.district}
          </span>
        </div>
        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-stone-900/5 pt-3 dark:border-stone-100/5">
          {statusCell(order, receivedAt)}
          {deliveryCell(order)}
        </div>
      </div>
    );
  };

  return (
    <div className="overflow-hidden rounded-3xl border border-stone-100 bg-linear-to-b from-white to-stone-50/60 shadow-(--shadow-soft) dark:border-stone-800 dark:from-stone-900 dark:to-stone-900">
      <div className="hidden items-center justify-end border-b border-stone-100 p-4 lg:flex dark:border-stone-800">
        <ViewModeToggle value={viewMode} onChange={setViewMode} ns="admin" />
      </div>

      {/* Десктоп/планшет — компактная таблица (6 колонок), без
          горизонтального скролла на типичном ноутбучном экране. */}
      <div className={viewMode === "table" ? "hidden overflow-x-auto lg:block" : "hidden"}>
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
              <th className="px-6 py-4 font-medium">{t("orders.columns.orderNumber")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.participants")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.products")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.region")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.amount")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.status")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.delivery")}</th>
            </tr>
          </thead>
          <tbody>
            {pageItems.map((order) => {
              const receivedAt = resolveReceivedAt(order.status, order.completedAt, deliveriesByOrderId.get(order.id)?.deliveredAt);
              const items = orderItems?.filter((i) => i.orderId === order.id) ?? [];
              return (
                <tr key={order.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                  <td className="px-6 py-4">
                    <span className="font-medium text-stone-800 dark:text-stone-100" title={order.orderNumber}>
                      {sequenceById.get(order.id) ?? "—"}
                    </span>
                    <span className="mt-0.5 block text-xs text-stone-400 dark:text-stone-500">{formatDateTime(order.createdAt)}</span>
                  </td>
                  <td className="max-w-48 px-6 py-4 text-stone-600 dark:text-stone-300">{participants(order)}</td>
                  <td className="px-6 py-4 text-stone-500 dark:text-stone-400">
                    <OrderItemsCell items={items} ns="admin" />
                  </td>
                  <td className="max-w-32 truncate px-6 py-4 text-stone-500 dark:text-stone-400">
                    {order.region}, {order.district}
                  </td>
                  <td className="px-6 py-4 font-semibold text-stone-800 dark:text-stone-100">
                    {formatSomoni(order.totalAmount)} {t("common.somoni")}
                  </td>
                  <td className="px-6 py-4">{statusCell(order, receivedAt)}</td>
                  <td className="px-6 py-4">{deliveryCell(order)}</td>
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
    </div>
  );
}
