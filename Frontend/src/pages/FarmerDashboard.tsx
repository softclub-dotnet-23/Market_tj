import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { Package, ShoppingBag, Star, Wallet } from "lucide-react";
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { useTheme } from "@/context/ThemeContext";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { formatNumber, formatSomoni } from "@/lib/utils";
import { useFarmerDashboard, useFarmerProfile } from "@/data/farmer";

function Card({ className, children }: { className?: string; children: ReactNode }) {
  return (
    <div
      className={
        "rounded-3xl border border-stone-100 bg-white p-6 dark:border-stone-800 dark:bg-stone-900 " + (className ?? "")
      }
    >
      {children}
    </div>
  );
}

function StatCard({
  icon: Icon,
  accent,
  label,
  value,
}: {
  icon: typeof Package;
  accent: "grove" | "blue" | "orange" | "rose";
  label: string;
  value: string;
}) {
  const ACCENT = {
    grove: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
    blue: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
    orange: "bg-orange-100 text-orange-700 dark:bg-orange-900 dark:text-orange-300",
    rose: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
  } as const;
  return (
    <Card className="flex flex-col gap-4">
      <span className={`flex h-11 w-11 items-center justify-center rounded-2xl ${ACCENT[accent]}`}>
        <Icon size={20} />
      </span>
      <div>
        <p className="text-sm text-stone-500 dark:text-stone-400">{label}</p>
        <p className="font-display text-2xl text-stone-900 dark:text-stone-50">{value}</p>
      </div>
    </Card>
  );
}

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
              <AreaChart data={chartData} margin={{ top: 4, right: 0, bottom: 0, left: -12 }}>
                <defs>
                  <linearGradient id="farmer-revenue" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="#298a47" stopOpacity={0.35} />
                    <stop offset="100%" stopColor="#298a47" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid stroke={grid} vertical={false} />
                <XAxis dataKey="label" tickLine={false} axisLine={false} tick={{ fill: muted, fontSize: 12 }} />
                <YAxis tickLine={false} axisLine={false} tick={{ fill: muted, fontSize: 12 }} />
                <Tooltip
                  formatter={(value) => `${formatSomoni(Number(value))} ${t("common.somoni")}`}
                  contentStyle={{ background: surface, border: "1px solid rgba(11,11,11,0.10)", borderRadius: 12, fontSize: 12, color: ink }}
                />
                <Area type="monotone" dataKey="revenue" stroke="#298a47" strokeWidth={2} fill="url(#farmer-revenue)" />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </Card>

        <Card>
          <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("dashboard.topSelling")}</h2>
          {dashboard.topSellingOwnProducts.length === 0 ? (
            <p className="mt-4 text-sm text-stone-400 dark:text-stone-500">{t("dashboard.noSalesYet")}</p>
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
