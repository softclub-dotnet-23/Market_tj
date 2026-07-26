import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { ShoppingCart } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { StatusMenu } from "@/components/ui/StatusMenu";
import { formatDateTime, formatSomoni } from "@/lib/utils";
import { ORDER_STATUS_CLASSES, ORDER_STATUS_ICONS, ORDER_STATUS_KEYS, getAdminNextStatuses, resolveReceivedAt } from "@/lib/orderStatus";
import { notifyCustomerAboutOrderStatus, updateOrderStatus, useAdminOrders, type AdminOrderDto } from "@/data/adminEntities";
// Delivery — общий для всех ролей список (та же схема, что уже используется
// у Farmer/Customer): грузим все записи о доставке и сопоставляем с заказом
// по orderId, чтобы показать реальное время получения.
import { useDeliveriesByOrder } from "@/data/farmer";

const PAGE_SIZE = 10;

export function AdminOrders() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const [refreshKey, setRefreshKey] = useState(0);
  const [busyId, setBusyId] = useState<number | null>(null);
  const { orders, loading, error } = useAdminOrders(refreshKey);
  const { deliveriesByOrderId, loading: deliveriesLoading } = useDeliveriesByOrder();

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

  return (
    <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
              <th className="px-6 py-4 font-medium">{t("orders.columns.orderNumber")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.customer")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.farmer")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.region")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.amount")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.status")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.createdAt")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.receivedAt")}</th>
            </tr>
          </thead>
          <tbody>
            {pageItems.map((order) => {
              const receivedAt = resolveReceivedAt(order.status, order.completedAt, deliveriesByOrderId.get(order.id)?.deliveredAt);
              return (
                <tr key={order.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                  <td className="px-6 py-4 font-medium text-stone-800 dark:text-stone-100">{order.orderNumber}</td>
                  <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{t("orders.customerLabel", { id: order.customerId })}</td>
                  <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{t("orders.farmerLabel", { id: order.farmerId })}</td>
                  <td className="px-6 py-4 text-stone-500 dark:text-stone-400">
                    {order.region}, {order.district}
                  </td>
                  <td className="px-6 py-4 font-semibold text-stone-800 dark:text-stone-100">
                    {formatSomoni(order.totalAmount)} {t("common.somoni")}
                  </td>
                  <td className="px-6 py-4">
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
                  </td>
                  <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDateTime(order.createdAt)}</td>
                  <td className="px-6 py-4 text-stone-500 dark:text-stone-400">
                    {receivedAt ? formatDateTime(receivedAt) : <span className="text-stone-300 dark:text-stone-600">{t("orders.notReceivedYet")}</span>}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="border-t border-stone-100 p-4 dark:border-stone-800">
          <Pagination page={currentPage} totalPages={totalPages} onPageChange={setPage} />
        </div>
      )}
    </div>
  );
}
