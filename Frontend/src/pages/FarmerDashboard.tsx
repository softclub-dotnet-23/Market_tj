import { useTranslation } from "react-i18next";
import { Package, ShoppingBag, Sprout, Star, Wallet } from "lucide-react";
import { Bar, BarChart, CartesianGrid, LabelList, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { useTheme } from "@/context/ThemeContext";
import { PageLoader } from "@/components/layout/PageLoader";
import { Card } from "@/components/ui/Card";
import { StatCard } from "@/components/ui/StatCard";
import { EmptyState } from "@/components/ui/EmptyState";
import { computeMonthlyTrend, formatNumber, formatSomoni } from "@/lib/utils";
import { useFarmerDashboard, useFarmerProfile } from "@/data/farmer";

export function FarmerDashboard() {
  const { t } = useTranslation("farmer");
  const { theme } = useTheme();
  const isDark = theme === "dark";
  const { profile, loading: profileLoading, error: profileError } = useFarmerProfile();
  const { data: dashboard, loading: dashboardLoading, error: dashboardError } = useFarmerDashboard(profile?.id ?? null);

  if (profileLoading || (profile && dashboardLoading)) return <PageLoader />;

  if (profileError || dashboardError || !profile || !dashboard) {
    return (
      <EmptyState
        icon={<Package size={26} />}
        title={t("dashboard.errorTitle")}
        description={profileError ?? dashboardError ?? t("dashboard.errorDescription")}
      />
    );
  }

  const months = t("dashboard.months", { returnObjects: true }) as string[];
  const chartData = dashboard.revenueByMonth.map((m) => ({
    label: months[m.month - 1] ?? m.month,
    revenue: m.revenue,
  }));
  const revenueTrend = computeMonthlyTrend(dashboard.revenueByMonth);
  const grid = isDark ? "#2c2c2a" : "#e1e0d9";
  const muted = "#898781";
  const surface = isDark ? "#1a1a19" : "#fcfcfb";
  const ink = isDark ? "#ffffff" : "#0b0b0b";

  return (
    <div className="flex flex-col gap-6">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <StatCard icon={Package} accent="grove" label={t("dashboard.stats.totalProducts")} value={formatNumber(dashboard.totalOwnProducts)} />
        <StatCard icon={Package} accent="blue" label={t("dashboard.stats.activeProducts")} value={formatNumber(dashboard.activeProducts)} />
        <StatCard icon={ShoppingBag} accent="orange" label={t("dashboard.stats.ordersThisMonth")} value={formatNumber(dashboard.ordersThisMonth)} />
        <StatCard
          icon={Wallet}
          accent="grove"
          label={t("dashboard.stats.revenueThisMonth")}
          value={`${formatSomoni(dashboard.revenueThisMonth)} ${t("common.somoni")}`}
          trend={revenueTrend}
          compareLabel={t("dashboard.vsLastMonth")}
        />
        <StatCard
          icon={Wallet}
          accent="blue"
          label={t("dashboard.stats.totalRevenue")}
          value={`${formatSomoni(dashboard.totalRevenue)} ${t("common.somoni")}`}
        />
        <StatCard
          icon={Star}
          accent="rose"
          label={t("dashboard.stats.averageRating")}
          value={dashboard.averageRating != null ? dashboard.averageRating.toFixed(1) : t("dashboard.noRatingYet")}
        />
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1.4fr_1fr]">
        <Card>
          <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("dashboard.revenueByMonth")}</h2>
          <div className="mt-4 h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={chartData} margin={{ top: 24, right: 0, bottom: 0, left: -12 }}>
                <defs>
                  <linearGradient id="farmer-revenue" x1="0" y1="0" x2="0" y2="1">
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
                <Bar dataKey="revenue" fill="url(#farmer-revenue)" radius={[6, 6, 0, 0]} maxBarSize={36}>
                  <LabelList dataKey="revenue" position="top" formatter={(v) => formatNumber(Number(v ?? 0))} style={{ fill: muted, fontSize: 11 }} />
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </Card>

        <Card>
          <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("dashboard.topSelling")}</h2>
          {dashboard.topSellingOwnProducts.length === 0 ? (
            <div className="mt-4 flex flex-col items-center gap-3 py-6 text-center">
              <span className="flex h-16 w-16 items-center justify-center rounded-full bg-grove-50 text-grove-300 dark:bg-grove-950 dark:text-grove-700">
                <Sprout size={28} />
              </span>
              <div>
                <p className="text-sm font-medium text-stone-700 dark:text-stone-200">{t("dashboard.noSalesYet")}</p>
                <p className="mt-1 text-xs text-stone-400 dark:text-stone-500">{t("dashboard.noSalesYetDescription")}</p>
              </div>
            </div>
          ) : (
            <ul className="mt-4 flex flex-col gap-4">
              {dashboard.topSellingOwnProducts.map((p) => (
                <li key={p.productName} className="flex items-center justify-between gap-3">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{p.productName}</p>
                    <p className="text-xs text-stone-400 dark:text-stone-500">
                      {formatNumber(p.quantitySold)} {t("dashboard.kg")}
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
    </div>
  );
}
