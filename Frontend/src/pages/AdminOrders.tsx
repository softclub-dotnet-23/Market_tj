import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { ShoppingCart } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { StatusMenu } from "@/components/ui/StatusMenu";
import { OrderItemsCell } from "@/components/ui/OrderItemsCell";
import { OrderItemsPhotoList } from "@/components/ui/OrderItemsPhotoList";
import { formatDateTime, formatSomoni } from "@/lib/utils";
import { ORDER_STATUS_CLASSES, ORDER_STATUS_ICONS, ORDER_STATUS_KEYS, getAdminNextStatuses, resolveReceivedAt } from "@/lib/orderStatus";
import {
  markAdminOrdersSeen,
  notifyCustomerAboutOrderStatus,
  updateOrderStatus,
  useAdminOrderItems,
  useAdminOrders,
  type AdminOrderDto,
} from "@/data/adminEntities";
// Delivery — общий для всех ролей список (та же схема, что уже используется
// у Farmer/Customer): грузим все записи о доставке и сопоставляем с заказом
// по orderId, чтобы показать реальное время получения.
import { useDeliveriesByOrder } from "@/data/farmer";
import { useProducts } from "@/data/products";

const PAGE_SIZE = 10;

export function AdminOrders() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const [refreshKey, setRefreshKey] = useState(0);
  const [busyId, setBusyId] = useState<number | null>(null);
  const { orders, loading, error } = useAdminOrders(refreshKey);
  const { deliveriesByOrderId, loading: deliveriesLoading } = useDeliveriesByOrder();
  const { orderItems } = useAdminOrderItems();
  const products = useProducts();
  const photoByListingId = new Map(products.map((p) => [p.id, p.photoUrl]));

  // Всё, что видно на этой странице, считается просмотренным — бейдж
  // "Заказы" в сайдбаре (AdminLayout) сразу пересчитывается через
  // markAdminOrdersSeen (см. data/adminEntities.ts), без перезагрузки.
  useEffect(() => {
    if (orders) markAdminOrdersSeen(orders);
  }, [orders]);

  if (loading || deliveriesLoading) return <PageLoader />;

  if (error || !orders) {
    return <EmptyState icon={<ShoppingCart size={26} />} title={t("orders.errorTitle")} description={error ?? t("orders.errorDescription")} />;
  }

  if (orders.length === 0) {
    return <EmptyState icon={<ShoppingCart size={26} />} title={t("orders.emptyTitle")} description={t("orders.emptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(orders.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminOrderDto[] = orders.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

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
      <span>
        <span className="text-stone-400 dark:text-stone-500">{t("orders.columns.customer")}: </span>
        {order.customerFullName ?? t("orders.customerLabel", { id: order.customerId })}
      </span>
      <span>
        <span className="text-stone-400 dark:text-stone-500">{t("orders.columns.farmer")}: </span>
        {t("orders.farmerLabel", { id: order.farmerId })}
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
        {receivedAt ? t("orders.columns.receivedAt") + ": " + formatDateTime(receivedAt) : t("orders.notReceivedYet")}
      </span>
    </div>
  );

  const renderCard = (order: AdminOrderDto) => {
    const receivedAt = resolveReceivedAt(order.status, order.completedAt, deliveriesByOrderId.get(order.id)?.deliveredAt);
    const items = orderItems?.filter((i) => i.orderId === order.id) ?? [];
    return (
      <div key={order.id} className="flex flex-col gap-3 rounded-2xl border border-stone-100 p-5 dark:border-stone-800">
        <div className="flex items-start justify-between gap-3">
          <div>
            <p className="font-medium text-stone-800 dark:text-stone-100">{order.orderNumber}</p>
            <p className="text-xs text-stone-400 dark:text-stone-500">{formatDateTime(order.createdAt)}</p>
          </div>
          <p className="font-semibold text-stone-800 dark:text-stone-100">
            {formatSomoni(order.totalAmount)} {t("common.somoni")}
          </p>
        </div>
        <div className="text-sm text-stone-600 dark:text-stone-300">{participants(order)}</div>
        <div className="border-t border-stone-50 pt-3 dark:border-stone-800/60">
          <OrderItemsPhotoList items={items} photoByListingId={photoByListingId} />
        </div>
        <div className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1.5 border-t border-stone-50 pt-3 text-sm dark:border-stone-800/60">
          <span className="text-stone-400 dark:text-stone-500">{t("orders.columns.region")}</span>
          <span className="text-stone-700 dark:text-stone-200">
            {order.region}, {order.district}
          </span>
        </div>
        <div className="pt-1">{statusCell(order, receivedAt)}</div>
      </div>
    );
  };

  return (
    <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
      {/* Десктоп/планшет — компактная таблица (6 колонок), без
          горизонтального скролла на типичном ноутбучном экране. */}
      <div className="hidden overflow-x-auto lg:block">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
              <th className="px-6 py-4 font-medium">{t("orders.columns.orderNumber")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.participants")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.products")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.region")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.amount")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.status")}</th>
            </tr>
          </thead>
          <tbody>
            {pageItems.map((order) => {
              const receivedAt = resolveReceivedAt(order.status, order.completedAt, deliveriesByOrderId.get(order.id)?.deliveredAt);
              const items = orderItems?.filter((i) => i.orderId === order.id) ?? [];
              return (
                <tr key={order.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                  <td className="px-6 py-4">
                    <span className="font-medium text-stone-800 dark:text-stone-100">{order.orderNumber}</span>
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
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Мобильный/узкий экран — карточки вместо таблицы, с фото товаров. */}
      <div className="grid grid-cols-1 gap-4 p-5 lg:hidden">{pageItems.map(renderCard)}</div>

      {totalPages > 1 && (
        <div className="border-t border-stone-100 p-4 dark:border-stone-800">
          <Pagination page={currentPage} totalPages={totalPages} onPageChange={setPage} />
        </div>
      )}
    </div>
  );
}
