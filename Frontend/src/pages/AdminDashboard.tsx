import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { ArrowDownRight, ArrowUpRight, Minus, ShoppingBag, Star } from "lucide-react";
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { useTheme } from "@/context/ThemeContext";
import { Avatar } from "@/components/ui/Avatar";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { resolveMediaUrl } from "@/lib/api";
import { cn, formatNumber, formatSomoni, timeAgo } from "@/lib/utils";
import { ORDER_STATUS_CLASSES, ORDER_STATUS_ICONS, ORDER_STATUS_KEYS } from "@/lib/orderStatus";
import { useAdminAnalytics, useAdminCustomers, useAdminOrders, type AdminAnalyticsDto } from "@/data/adminEntities";
import { useFarmers } from "@/data/farmers";
import { useCategories } from "@/data/categories";
import { useCatalogLoaded } from "@/data/products";
import {
  computePopularCategories,
  computeRecentOrders,
  computeRegionSales,
  computeTopFarmers,
  getRegionColor,
  useAdminStats,
  useOrdersByMonth,
  type AdminStat,
  type PopularCategoryRow,
  type RecentOrder,
  type RegionSales,
  type SiteVisitPoint,
  type TopFarmerRow,
} from "@/data/admin";

const EMPTY_ANALYTICS: AdminAnalyticsDto = {
  totalUsers: 0,
  totalFarmers: 0,
  totalCustomers: 0,
  totalCouriers: 0,
  totalOrders: 0,
  ordersToday: 0,
  ordersThisMonth: 0,
  totalRevenue: 0,
  revenueThisMonth: 0,
  totalProductListings: 0,
  activeProductListings: 0,
  topSellingProducts: [],
  ordersByStatus: [],
  revenueByMonth: [],
};

const ACCENT_CLASSES = {
  grove: { icon: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300", line: "#298a47" },
  blue: { icon: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300", line: "#2a78d6" },
  orange: { icon: "bg-orange-100 text-orange-700 dark:bg-orange-900 dark:text-orange-300", line: "#ea7a1f" },
  rose: { icon: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300", line: "#e0507a" },
} as const;

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

function StatCard({ stat }: { stat: AdminStat }) {
  const { t } = useTranslation("admin");
  const accent = ACCENT_CLASSES[stat.accent];
  const chartData = stat.trend.map((v, i) => ({ i, v }));
  const displayValue =
    stat.key === "revenue" ? `${formatNumber(stat.value)} ${stat.suffix}` : formatNumber(stat.value);
  const positive = stat.changePercent !== null && stat.changePercent > 0;
  const negative = stat.changePercent !== null && stat.changePercent < 0;

  return (
    <Card className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <span className={`flex h-11 w-11 items-center justify-center rounded-2xl ${accent.icon}`}>
          <stat.icon size={20} />
        </span>
        <span
          className={cn(
            "flex items-center gap-1 text-xs font-semibold",
            positive
              ? "text-grove-600 dark:text-grove-400"
              : negative
                ? "text-rose-600 dark:text-rose-400"
                : "text-stone-400 dark:text-stone-500",
          )}
        >
          {positive ? <ArrowUpRight size={13} /> : negative ? <ArrowDownRight size={13} /> : <Minus size={13} />}
          {stat.changePercent !== null ? `${Math.abs(stat.changePercent).toFixed(1)}%` : t("dashboard.newMetric")}
        </span>
      </div>
      <div>
        <p className="text-sm text-stone-500 dark:text-stone-400">{t(`dashboard.stats.${stat.key}`)}</p>
        <p className="font-display text-2xl text-stone-900 dark:text-stone-50">{displayValue}</p>
      </div>
      <div className="flex items-center justify-between gap-3">
        <p className="text-xs text-stone-400 dark:text-stone-500">
          {t("dashboard.vsLastMonth", { value: stat.compareValue })}
        </p>
        <div className="h-8 w-20">
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={chartData} margin={{ top: 2, right: 0, bottom: 0, left: 0 }}>
              <defs>
                <linearGradient id={`spark-${stat.key}`} x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor={accent.line} stopOpacity={0.35} />
                  <stop offset="100%" stopColor={accent.line} stopOpacity={0} />
                </linearGradient>
              </defs>
              <Area type="monotone" dataKey="v" stroke={accent.line} strokeWidth={2} fill={`url(#spark-${stat.key})`} />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      </div>
    </Card>
  );
}

function RegionDonutCard({ regions }: { regions: RegionSales[] }) {
  const { t } = useTranslation("admin");
  const { theme } = useTheme();
  const isDark = theme === "dark";
  const total = regions.reduce((sum, r) => sum + r.amount, 0);
  const surface = isDark ? "#1a1a19" : "#fcfcfb";
  const ink = isDark ? "#ffffff" : "#0b0b0b";
  const muted = "#898781";

  return (
    <Card>
      <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("dashboard.salesByRegion")}</h2>
      {regions.length === 0 ? (
        <p className="mt-4 text-sm text-stone-400 dark:text-stone-500">{t("statistics.noRevenueYet")}</p>
      ) : (
        <div className="mt-4 flex flex-col items-center gap-6 sm:flex-row">
          <div className="relative h-52 w-52 shrink-0">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={regions}
                  dataKey="amount"
                  nameKey="name"
                  innerRadius="68%"
                  outerRadius="100%"
                  paddingAngle={2}
                  stroke={surface}
                  strokeWidth={2}
                >
                  {regions.map((r, i) => (
                    <Cell key={r.key} fill={getRegionColor(i, isDark)} />
                  ))}
                </Pie>
                <Tooltip
                  formatter={(value) => [`${formatSomoni(Number(value))} ${t("common.somoni")}`, ""]}
                  contentStyle={{
                    background: surface,
                    border: "1px solid rgba(11,11,11,0.10)",
                    borderRadius: 12,
                    fontSize: 12,
                    color: ink,
                  }}
                />
              </PieChart>
            </ResponsiveContainer>
            <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
              <p className="text-xs text-stone-400 dark:text-stone-500">{t("dashboard.total")}</p>
              <p className="font-display text-xl text-stone-900 dark:text-stone-50">{formatNumber(total)}</p>
            </div>
          </div>

          <ul className="flex w-full flex-col gap-2.5">
            {regions.map((r, i) => (
              <li key={r.key} className="flex items-center gap-2.5 text-sm">
                <span
                  className="h-2.5 w-2.5 shrink-0 rounded-full"
                  style={{ backgroundColor: getRegionColor(i, isDark) }}
                />
                <span className="w-32 shrink-0 truncate text-stone-600 dark:text-stone-300">{r.name}</span>
                <span className="flex-1 text-right whitespace-nowrap text-stone-400 dark:text-stone-500" style={{ color: muted }}>
                  {formatNumber(r.amount)} {t("common.somoni")}
                </span>
                <span className="w-11 shrink-0 text-right font-semibold tabular-nums text-stone-800 dark:text-stone-100">
                  {r.percent}%
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </Card>
  );
}

function OrdersByMonthCard({ visits }: { visits: SiteVisitPoint[] }) {
  const { t } = useTranslation("admin");
  const { theme } = useTheme();
  const isDark = theme === "dark";
  const surface = isDark ? "#1a1a19" : "#fcfcfb";
  const grid = isDark ? "#2c2c2a" : "#e1e0d9";
  const muted = "#898781";
  const ink = isDark ? "#ffffff" : "#0b0b0b";
  const currentYear = new Date().getFullYear();
  const previousYear = currentYear - 1;

  return (
    <Card>
      <div className="flex items-center justify-between">
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("dashboard.ordersByMonth")}</h2>
        <div className="flex items-center gap-4 text-xs text-stone-500 dark:text-stone-400">
          <span className="flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-full bg-grove-600" />
            {currentYear}
          </span>
          <span className="flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-full bg-grove-200 dark:bg-grove-800" />
            {previousYear}
          </span>
        </div>
      </div>
      <div className="mt-4 h-64">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={visits} barGap={4} margin={{ top: 4, right: 0, bottom: 0, left: -12 }}>
            <CartesianGrid stroke={grid} vertical={false} />
            <XAxis dataKey="month" tickLine={false} axisLine={false} tick={{ fill: muted, fontSize: 12 }} />
            <YAxis tickLine={false} axisLine={false} tick={{ fill: muted, fontSize: 12 }} allowDecimals={false} />
            <Tooltip
              cursor={{ fill: isDark ? "rgba(255,255,255,0.04)" : "rgba(11,11,11,0.04)" }}
              formatter={(value) => formatNumber(Number(value))}
              contentStyle={{
                background: surface,
                border: "1px solid rgba(11,11,11,0.10)",
                borderRadius: 12,
                fontSize: 12,
                color: ink,
              }}
            />
            <Bar dataKey="previous" fill={isDark ? "#1f5731" : "#c3ebcc"} radius={[4, 4, 0, 0]} maxBarSize={16} />
            <Bar dataKey="current" fill="#298a47" radius={[4, 4, 0, 0]} maxBarSize={16} />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </Card>
  );
}

function RecentOrdersCard({ orders }: { orders: RecentOrder[] }) {
  const { t } = useTranslation("admin");
  return (
    <Card>
      <div className="flex items-center justify-between">
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("dashboard.recentOrders")}</h2>
        <button className="text-xs font-semibold text-grove-700 hover:text-grove-800 dark:text-grove-400">
          {t("dashboard.viewAll")}
        </button>
      </div>
      {orders.length === 0 ? (
        <p className="mt-4 text-sm text-stone-400 dark:text-stone-500">{t("statistics.noOrdersYet")}</p>
      ) : (
        <ul className="mt-4 flex flex-col gap-4">
          {orders.map((order) => {
            const Icon = ORDER_STATUS_ICONS[order.status];
            return (
              <li key={order.orderNumber} className="flex items-center gap-3">
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">
                    {order.orderNumber} · {t("orders.customerLabel", { id: order.customerId })}
                  </p>
                  <p className="truncate text-xs text-stone-400 dark:text-stone-500">
                    {timeAgo(order.createdAt)}
                    <br />
                    {order.region}, {order.district}
                  </p>
                </div>
                <div className="flex shrink-0 flex-col items-end gap-1.5">
                  <p className="text-sm font-semibold text-stone-800 dark:text-stone-100">
                    {formatSomoni(order.amount)} {t("common.somoni")}
                  </p>
                  <span
                    className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-semibold ${ORDER_STATUS_CLASSES[order.status]}`}
                  >
                    {Icon && <Icon size={11} />}
                    {t(`orders.status.${ORDER_STATUS_KEYS[order.status]}`)}
                  </span>
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </Card>
  );
}

function TopFarmersCard({ farmers }: { farmers: TopFarmerRow[] }) {
  const { t } = useTranslation("admin");
  return (
    <Card>
      <div className="flex items-center justify-between">
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("dashboard.topFarmers")}</h2>
        <button className="text-xs font-semibold text-grove-700 hover:text-grove-800 dark:text-grove-400">
          {t("dashboard.viewAll")}
        </button>
      </div>
      {farmers.length === 0 ? (
        <p className="mt-4 text-sm text-stone-400 dark:text-stone-500">{t("dashboard.noTopFarmersYet")}</p>
      ) : (
        <ul className="mt-4 flex flex-col gap-4">
          {farmers.map((farmer) => (
            <li key={farmer.id} className="flex items-center gap-3">
              <Avatar name={farmer.name} src={farmer.avatarUrl ? resolveMediaUrl(farmer.avatarUrl) : undefined} size={40} />
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{farmer.name}</p>
                <p className="truncate text-xs text-stone-400 dark:text-stone-500">{farmer.region}</p>
              </div>
              <div className="flex shrink-0 flex-col items-end gap-1">
                <span className="flex items-center gap-1 text-xs font-semibold text-stone-700 dark:text-stone-200">
                  <Star size={12} className="fill-harvest-400 text-harvest-400" />
                  {farmer.rating.toFixed(1)}
                </span>
                <p className="text-xs text-stone-400 dark:text-stone-500">
                  {formatNumber(farmer.revenue)} {t("common.somoni")}
                </p>
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

function PopularCategoriesCard({ categories }: { categories: PopularCategoryRow[] }) {
  const { t } = useTranslation("admin");
  return (
    <Card>
      <div className="flex items-center justify-between">
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("dashboard.popularCategories")}</h2>
        <button className="text-xs font-semibold text-grove-700 hover:text-grove-800 dark:text-grove-400">
          {t("dashboard.viewAll")}
        </button>
      </div>
      {categories.length === 0 ? (
        <p className="mt-5 text-sm text-stone-400 dark:text-stone-500">{t("dashboard.noPopularCategoriesYet")}</p>
      ) : (
        <ul className="mt-5 flex flex-col gap-4">
          {categories.map((category) => (
            <li key={category.slug} className="flex items-center gap-3">
              <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-grove-50 text-grove-700 dark:bg-grove-950 dark:text-grove-300">
                <category.icon size={16} />
              </span>
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm text-stone-700 dark:text-stone-200">{category.name}</p>
                <div className="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-stone-100 dark:bg-stone-800">
                  <div className="h-full rounded-full bg-grove-600" style={{ width: `${category.percent}%` }} />
                </div>
              </div>
              <span className="shrink-0 text-sm font-semibold tabular-nums text-stone-800 dark:text-stone-100">
                {category.percent}%
              </span>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

export function AdminDashboard() {
  const { t } = useTranslation("admin");
  const { analytics, loading: analyticsLoading, error: analyticsError } = useAdminAnalytics();
  const { orders, loading: ordersLoading, error: ordersError } = useAdminOrders();
  const { customers, loading: customersLoading, error: customersError } = useAdminCustomers();
  const farmers = useFarmers();
  const categories = useCategories();
  const catalogLoaded = useCatalogLoaded();

  // Хуки должны вызываться безусловно на каждом рендере (Rules of Hooks) —
  // поэтому считаем производные данные ДО проверки на loading/error, с
  // безопасными пустыми значениями, пока реальные данные ещё не пришли.
  const stats = useAdminStats(analytics ?? EMPTY_ANALYTICS, orders ?? [], customers ?? [], farmers);
  const visits = useOrdersByMonth(orders ?? []);

  if (analyticsLoading || ordersLoading || customersLoading || !catalogLoaded) return <PageLoader />;

  const error = analyticsError ?? ordersError ?? customersError;
  if (error || !analytics || !orders || !customers) {
    return <EmptyState icon={<ShoppingBag size={26} />} title={t("dashboard.errorTitle")} description={error ?? t("dashboard.errorDescription")} />;
  }

  const regions = computeRegionSales(orders);
  const recentOrders = computeRecentOrders(orders);
  const topFarmers = computeTopFarmers(orders, farmers);
  const popularCategories = computePopularCategories(categories);

  return (
    <div className="flex flex-col gap-6">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {stats.map((stat) => (
          <StatCard key={stat.key} stat={stat} />
        ))}
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1fr_1.3fr]">
        <RegionDonutCard regions={regions} />
        <OrdersByMonthCard visits={visits} />
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <RecentOrdersCard orders={recentOrders} />
        <TopFarmersCard farmers={topFarmers} />
        <PopularCategoriesCard categories={popularCategories} />
      </div>
    </div>
  );
}
