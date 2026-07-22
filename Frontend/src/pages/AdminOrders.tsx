import { useState } from "react";
import { useTranslation } from "react-i18next";
import { ShoppingCart } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { formatDate, formatSomoni } from "@/lib/utils";
import { OrderStatus, useAdminOrders, type AdminOrderDto } from "@/data/adminEntities";

const PAGE_SIZE = 10;

const STATUS_CLASSES: Record<number, string> = {
  [OrderStatus.Pending]: "bg-stone-100 text-stone-600 dark:bg-stone-800 dark:text-stone-300",
  [OrderStatus.Accepted]: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  [OrderStatus.Rejected]: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
  [OrderStatus.Preparing]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [OrderStatus.ReadyForPickup]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [OrderStatus.CourierAssigned]: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  [OrderStatus.PickedUp]: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  [OrderStatus.InDelivery]: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  [OrderStatus.Delivered]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [OrderStatus.Completed]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [OrderStatus.Cancelled]: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
};

const STATUS_KEYS: Record<number, string> = {
  [OrderStatus.Pending]: "pending",
  [OrderStatus.Accepted]: "accepted",
  [OrderStatus.Rejected]: "rejected",
  [OrderStatus.Preparing]: "preparing",
  [OrderStatus.ReadyForPickup]: "readyForPickup",
  [OrderStatus.CourierAssigned]: "courierAssigned",
  [OrderStatus.PickedUp]: "pickedUp",
  [OrderStatus.InDelivery]: "inDelivery",
  [OrderStatus.Delivered]: "delivered",
  [OrderStatus.Completed]: "completed",
  [OrderStatus.Cancelled]: "cancelled",
};

export function AdminOrders() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const { orders, loading, error } = useAdminOrders();

  if (loading) return <PageLoader />;

  if (error || !orders) {
    return <EmptyState icon={<ShoppingCart size={26} />} title={t("orders.errorTitle")} description={error ?? t("orders.errorDescription")} />;
  }

  if (orders.length === 0) {
    return <EmptyState icon={<ShoppingCart size={26} />} title={t("orders.emptyTitle")} description={t("orders.emptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(orders.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminOrderDto[] = orders.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

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
            </tr>
          </thead>
          <tbody>
            {pageItems.map((order) => (
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
                  <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_CLASSES[order.status] ?? STATUS_CLASSES[OrderStatus.Pending]}`}>
                    {t(`orders.status.${STATUS_KEYS[order.status] ?? "pending"}`)}
                  </span>
                </td>
                <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(order.createdAt)}</td>
              </tr>
            ))}
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
