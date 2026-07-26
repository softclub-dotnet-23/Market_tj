import { useState } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { ShoppingBag, Star } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { Button } from "@/components/ui/Button";
import { ReviewModal } from "@/components/customer/ReviewModal";
import { formatDateTime, formatSomoni } from "@/lib/utils";
import { ORDER_STATUS_CLASSES, ORDER_STATUS_ICONS, ORDER_STATUS_KEYS, OrderStatus, resolveReceivedAt } from "@/lib/orderStatus";
import {
  submitCustomerReview,
  useCustomerOrders,
  useCustomerProfile,
  useCustomerReviewedOrderIds,
  type CustomerOrderDto,
} from "@/data/customer";
// Delivery привязан к заказу через OrderId, не через customerId — тот же
// generic-хук, что уже используется в FarmerOrders.tsx, просто ещё один
// потребитель того же общего списка доставок.
import { useDeliveriesByOrder } from "@/data/farmer";

const PAGE_SIZE = 10;

export function CustomerOrders() {
  const { t } = useTranslation("customer");
  const [page, setPage] = useState(1);
  const [reviewRefreshKey, setReviewRefreshKey] = useState(0);
  const [reviewingOrder, setReviewingOrder] = useState<CustomerOrderDto | null>(null);
  const { profile, loading: profileLoading, error: profileError } = useCustomerProfile();
  const { orders, loading: ordersLoading, error: ordersError } = useCustomerOrders(profile?.id ?? null);
  const { reviewedOrderIds } = useCustomerReviewedOrderIds(profile?.id ?? null, reviewRefreshKey);
  const { deliveriesByOrderId, loading: deliveriesLoading } = useDeliveriesByOrder();

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

  return (
    <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
              <th className="px-6 py-4 font-medium">{t("orders.columns.orderNumber")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.address")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.amount")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.status")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.createdAt")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.receivedAt")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.review")}</th>
            </tr>
          </thead>
          <tbody>
            {pageItems.map((order) => {
              const alreadyReviewed = reviewedOrderIds.has(order.id);
              const receivedAt = resolveReceivedAt(order.status, order.completedAt, deliveriesByOrderId.get(order.id)?.deliveredAt);
              return (
                <tr key={order.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                  <td className="px-6 py-4 font-medium text-stone-800 dark:text-stone-100">{order.orderNumber}</td>
                  <td className="max-w-64 truncate px-6 py-4 text-stone-500 dark:text-stone-400">
                    {order.region}, {order.district}
                  </td>
                  <td className="px-6 py-4 font-semibold text-stone-800 dark:text-stone-100">
                    {formatSomoni(order.totalAmount)} {t("common.somoni")}
                  </td>
                  <td className="px-6 py-4">
                    <StatusBadge
                      label={t(`orders.status.${ORDER_STATUS_KEYS[order.status] ?? "pending"}`)}
                      className={ORDER_STATUS_CLASSES[order.status] ?? ORDER_STATUS_CLASSES[OrderStatus.Pending]}
                      icon={ORDER_STATUS_ICONS[order.status]}
                    />
                  </td>
                  <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDateTime(order.createdAt)}</td>
                  <td className="px-6 py-4 text-stone-500 dark:text-stone-400">
                    {receivedAt ? formatDateTime(receivedAt) : <span className="text-stone-300 dark:text-stone-600">{t("orders.notReceivedYet")}</span>}
                  </td>
                  <td className="px-6 py-4">
                    {order.status !== OrderStatus.Completed ? (
                      <span className="text-stone-300 dark:text-stone-600">—</span>
                    ) : alreadyReviewed ? (
                      <span className="inline-flex items-center gap-1.5 text-xs font-semibold text-grove-700 dark:text-grove-400">
                        <Star size={13} fill="currentColor" />
                        {t("orders.alreadyReviewed")}
                      </span>
                    ) : (
                      <Button size="sm" variant="outline" leftIcon={<Star size={13} />} onClick={() => setReviewingOrder(order)}>
                        {t("orders.writeReview")}
                      </Button>
                    )}
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

      <ReviewModal
        open={reviewingOrder !== null}
        onClose={() => setReviewingOrder(null)}
        orderNumber={reviewingOrder?.orderNumber ?? ""}
        onSubmit={handleReviewSubmit}
      />
    </div>
  );
}
