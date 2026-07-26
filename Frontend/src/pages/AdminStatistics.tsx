import { useTranslation } from "react-i18next";
import { Bike, Package, ShoppingBag, Sprout, Users, Wallet } from "lucide-react";
import { Bar, BarChart, CartesianGrid, LabelList, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { useTheme } from "@/context/ThemeContext";
import { PageLoader } from "@/components/layout/PageLoader";
import { Card } from "@/components/ui/Card";
import { StatCard } from "@/components/ui/StatCard";
import { EmptyState } from "@/components/ui/EmptyState";
import { computeMonthlyTrend, formatNumber, formatSomoni } from "@/lib/utils";
import { OrderStatus, useAdminAnalytics } from "@/data/adminEntities";

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


export function AdminStatistics() {
  const { t } = useTranslation("admin");
  const { theme } = useTheme();
  const isDark = theme === "dark";
  const { analytics, loading, error } = useAdminAnalytics();

  if (loading) return <PageLoader />;

  if (error || !analytics) {
    return <EmptyState icon={<ShoppingBag size={26} />} title={t("statistics.errorTitle")} description={error ?? t("statistics.errorDescription")} />;
  }

  const months = t("statistics.months", { returnObjects: true }) as string[];
  const chartData = analytics.revenueByMonth.map((m) => ({ label: months[m.month - 1] ?? m.month, revenue: m.revenue }));
  const revenueTrend = computeMonthlyTrend(analytics.revenueByMonth);
  const maxStatusCount = Math.max(1, ...analytics.ordersByStatus.map((s) => s.count));
  const grid = isDark ? "#2c2c2a" : "#e1e0d9";
  const muted = "#898781";
  const surface = isDark ? "#1a1a19" : "#fcfcfb";
  const ink = isDark ? "#ffffff" : "#0b0b0b";

  return (
    <div className="flex flex-col gap-6">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard icon={Users} accent="blue" label={t("statistics.stats.totalUsers")} value={formatNumber(analytics.totalUsers)} />
        <StatCard icon={Sprout} accent="grove" label={t("statistics.stats.totalFarmers")} value={formatNumber(analytics.totalFarmers)} />
        <StatCard icon={ShoppingBag} accent="orange" label={t("statistics.stats.ordersThisMonth")} value={formatNumber(analytics.ordersThisMonth)} />
        <StatCard
          icon={Wallet}
          accent="grove"
          label={t("statistics.stats.revenueThisMonth")}
          value={`${formatSomoni(analytics.revenueThisMonth)} ${t("common.somoni")}`}
          trend={revenueTrend}
          compareLabel={t("statistics.vsLastMonth")}
        />
        <StatCard icon={Users} accent="blue" label={t("statistics.stats.totalCustomers")} value={formatNumber(analytics.totalCustomers)} />
        <StatCard icon={Bike} accent="rose" label={t("statistics.stats.totalCouriers")} value={formatNumber(analytics.totalCouriers)} />
        <StatCard icon={Package} accent="orange" label={t("statistics.stats.activeProductListings")} value={formatNumber(analytics.activeProductListings)} />
        <StatCard
          icon={Wallet}
          accent="grove"
          label={t("statistics.stats.totalRevenue")}
          value={`${formatSomoni(analytics.totalRevenue)} ${t("common.somoni")}`}
        />
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1.4fr_1fr]">
        <Card>
          <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("statistics.revenueByMonth")}</h2>
          {chartData.length === 0 ? (
            <p className="mt-4 text-sm text-stone-400 dark:text-stone-500">{t("statistics.noRevenueYet")}</p>
          ) : (
            <div className="mt-4 h-64">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={chartData} margin={{ top: 24, right: 0, bottom: 0, left: -12 }}>
                  <defs>
                    <linearGradient id="admin-stats-revenue" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="0%" stopColor="#3ba85a" />
                      <stop offset="100%" stopColor="#226e3a" />
                    </linearGradient>
                  </defs>
                  <CartesianGrid stroke={grid} vertical={false} />
                  <XAxis dataKey="label" tickLine={false} axisLine={false} tick={{ fill: muted, fontSize: 12 }} />
                  <YAxis tickLine={false} axisLine={false} tick={{ fill: muted, fontSize: 12 }} />
                  <Tooltip
                    formatter={(value) => `${formatSomoni(Number(value))} ${t("common.somoni")}`}
                    cursor={{ fill: isDark ? "rgba(255,255,255,0.04)" : "rgba(11,11,11,0.03)" }}
                    contentStyle={{ background: surface, border: "1px solid rgba(11,11,11,0.10)", borderRadius: 12, fontSize: 12, color: ink }}
                  />
                  <Bar dataKey="revenue" fill="url(#admin-stats-revenue)" radius={[6, 6, 0, 0]} maxBarSize={36}>
                    <LabelList dataKey="revenue" position="top" formatter={(v) => formatNumber(Number(v ?? 0))} style={{ fill: muted, fontSize: 11 }} />
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>
          )}
        </Card>

        <Card>
          <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("statistics.topSellingProducts")}</h2>
          {analytics.topSellingProducts.length === 0 ? (
            <div className="mt-4 flex flex-col items-center gap-3 py-6 text-center">
              <span className="flex h-16 w-16 items-center justify-center rounded-full bg-grove-50 text-grove-300 dark:bg-grove-950 dark:text-grove-700">
                <Sprout size={28} />
              </span>
              <div>
                <p className="text-sm font-medium text-stone-700 dark:text-stone-200">{t("statistics.noSalesYet")}</p>
                <p className="mt-1 text-xs text-stone-400 dark:text-stone-500">{t("statistics.noSalesYetDescription")}</p>
              </div>
            </div>
          ) : (
            <ul className="mt-4 flex flex-col gap-4">
              {analytics.topSellingProducts.map((p) => (
                <li key={p.productName} className="flex items-center justify-between gap-3">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{p.productName}</p>
                    <p className="text-xs text-stone-400 dark:text-stone-500">
                      {formatNumber(p.quantitySold)} {t("statistics.kg")}
                    </p>
                  </div>
                  <p className="shrink-0 text-sm font-semibold text-stone-800 dark:text-stone-100">
                    {formatSomoni(p.revenue)} {t("common.somoni")}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      <Card>
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("statistics.ordersByStatus")}</h2>
        {analytics.ordersByStatus.length === 0 ? (
          <p className="mt-4 text-sm text-stone-400 dark:text-stone-500">{t("statistics.noOrdersYet")}</p>
        ) : (
          <ul className="mt-5 flex flex-col gap-4">
            {analytics.ordersByStatus.map((s) => (
              <li key={s.status} className="flex items-center gap-3">
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm text-stone-700 dark:text-stone-200">
                    {t(`orders.status.${STATUS_KEYS[s.status] ?? "pending"}`)}
                  </p>
                  <div className="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-stone-100 dark:bg-stone-800">
                    <div className="h-full rounded-full bg-grove-600" style={{ width: `${(s.count / maxStatusCount) * 100}%` }} />
                  </div>
                </div>
                <span className="shrink-0 text-sm font-semibold tabular-nums text-stone-800 dark:text-stone-100">{formatNumber(s.count)}</span>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}
