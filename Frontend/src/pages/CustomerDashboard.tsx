import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ArrowRight, Clock, PackageCheck, ShoppingBag, Wallet } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { Card } from "@/components/ui/Card";
import { StatCard } from "@/components/ui/StatCard";
import { EmptyState } from "@/components/ui/EmptyState";
import { formatNumber, formatSomoni, formatDateTime } from "@/lib/utils";
import { OrderStatus, useCustomerOrders, useCustomerProfile } from "@/data/customer";

const ACTIVE_STATUSES = new Set<number>([
  OrderStatus.Pending,
  OrderStatus.Accepted,
  OrderStatus.Preparing,
  OrderStatus.ReadyForPickup,
  OrderStatus.CourierAssigned,
  OrderStatus.PickedUp,
  OrderStatus.InDelivery,
]);
const DONE_STATUSES = new Set<number>([OrderStatus.Delivered, OrderStatus.Completed]);

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


export function CustomerDashboard() {
  const { t } = useTranslation("customer");
  const { profile, loading: profileLoading, error: profileError } = useCustomerProfile();
  const { orders, loading: ordersLoading, error: ordersError } = useCustomerOrders(profile?.id ?? null);

  if (profileLoading || (profile && ordersLoading)) return <PageLoader />;

  if (profileError || ordersError || !profile || !orders) {
    return (
      <EmptyState
        icon={<ShoppingBag size={26} />}
        title={t("dashboard.errorTitle")}
        description={profileError ?? ordersError ?? t("dashboard.errorDescription")}
      />
    );
  }

  const activeCount = orders.filter((o) => ACTIVE_STATUSES.has(o.status)).length;
  const totalSpent = orders.filter((o) => DONE_STATUSES.has(o.status)).reduce((sum, o) => sum + o.totalAmount, 0);
  const recent = orders.slice(0, 5);

  return (
    <div className="flex flex-col gap-6">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <StatCard icon={ShoppingBag} accent="blue" label={t("dashboard.stats.totalOrders")} value={formatNumber(orders.length)} />
        <StatCard icon={Clock} accent="orange" label={t("dashboard.stats.activeOrders")} value={formatNumber(activeCount)} />
        <StatCard icon={Wallet} accent="grove" label={t("dashboard.stats.totalSpent")} value={`${formatSomoni(totalSpent)} ${t("common.somoni")}`} />
      </div>

      <Card>
        <div className="flex items-center justify-between">
          <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("dashboard.recentOrders")}</h2>
          <Link to="/customer/orders" className="flex items-center gap-1 text-xs font-semibold text-grove-700 hover:text-grove-800 dark:text-grove-400">
            {t("dashboard.viewAll")}
            <ArrowRight size={13} />
          </Link>
        </div>

        {recent.length === 0 ? (
          <div className="mt-6 flex flex-col items-center gap-3 py-8 text-center">
            <span className="flex h-12 w-12 items-center justify-center rounded-2xl bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500">
              <PackageCheck size={22} />
            </span>
            <p className="text-sm text-stone-500 dark:text-stone-400">{t("dashboard.noOrdersYet")}</p>
            <Link to="/catalog" className="mt-1 inline-flex items-center gap-2 rounded-xl bg-grove-700 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-grove-800">
              {t("dashboard.goToCatalog")}
              <ArrowRight size={14} />
            </Link>
          </div>
        ) : (
          <ul className="mt-4 flex flex-col divide-y divide-stone-50 dark:divide-stone-800/60">
            {recent.map((order) => (
              <li key={order.id} className="flex items-center justify-between gap-3 py-3.5">
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{order.orderNumber}</p>
                  <p className="text-xs text-stone-400 dark:text-stone-500">{formatDateTime(order.createdAt)}</p>
                </div>
                <div className="flex shrink-0 items-center gap-4">
                  <span className="text-sm font-semibold text-stone-800 dark:text-stone-100">
                    {formatSomoni(order.totalAmount)} {t("common.somoni")}
                  </span>
                  <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_CLASSES[order.status] ?? STATUS_CLASSES[OrderStatus.Pending]}`}>
                    {t(`orders.status.${STATUS_KEYS[order.status] ?? "pending"}`)}
                  </span>
                </div>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}
