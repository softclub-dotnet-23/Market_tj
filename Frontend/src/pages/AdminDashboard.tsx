import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { ArrowUpRight, Star } from "lucide-react";
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
import { formatNumber, formatSomoni } from "@/lib/utils";
import {
  getRegionColor,
  useAdminStats,
  usePopularCategories,
  useRecentOrders,
  useRegionSales,
  useSiteVisits,
  useTopFarmers,
  type AdminStat,
  type OrderStatus,
} from "@/data/admin";

const ACCENT_CLASSES = {
  grove: { icon: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300", line: "#298a47" },
  blue: { icon: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300", line: "#2a78d6" },
  orange: { icon: "bg-orange-100 text-orange-700 dark:bg-orange-900 dark:text-orange-300", line: "#ea7a1f" },
  rose: { icon: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300", line: "#e0507a" },
} as const;

const STATUS_CLASSES: Record<OrderStatus, string> = {
  new: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  processing: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  shipped: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  delivered: "bg-stone-100 text-stone-600 dark:bg-stone-800 dark:text-stone-300",
};

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

  return (
    <Card className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <span className={`flex h-11 w-11 items-center justify-center rounded-2xl ${accent.icon}`}>
          <stat.icon size={20} />
        </span>
        <span className="flex items-center gap-1 text-xs font-semibold text-grove-600 dark:text-grove-400">
          <ArrowUpRight size={13} />
          {stat.changePercent}%
        </span>
      </div>
      <div>
        <p className="text-sm text-stone-500 dark:text-stone-400">{t(`dashboard.stats.${stat.key}`)}</p>
        <p className="font-display text-2xl text-stone-900 dark:text-stone-50">{displayValue}</p>
      </div>
      <div className="flex items-center justify-between gap-3">
        <p className="text-xs text-stone-400 dark:text-stone-500">
          {t("dashboard.vsLastWeek", { value: stat.compareValue })}
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

function RegionDonutCard() {
  const { t } = useTranslation("admin");
  const { theme } = useTheme();
  const regions = useRegionSales();
  const isDark = theme === "dark";
  const total = regions.reduce((sum, r) => sum + r.amount, 0);
  const surface = isDark ? "#1a1a19" : "#fcfcfb";
  const ink = isDark ? "#ffffff" : "#0b0b0b";
  const muted = "#898781";

  return (
    <Card>
      <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("dashboard.salesByRegion")}</h2>
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
                formatter={(value: number) => [`${formatSomoni(value)} ${t("common.somoni")}`, ""]}
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
    </Card>
  );
}

function SiteVisitsCard() {
  const { t } = useTranslation("admin");
  const { theme } = useTheme();
  const isDark = theme === "dark";
  const visits = useSiteVisits();
  const surface = isDark ? "#1a1a19" : "#fcfcfb";
  const grid = isDark ? "#2c2c2a" : "#e1e0d9";
  const muted = "#898781";
  const ink = isDark ? "#ffffff" : "#0b0b0b";

  return (
    <Card>
      <div className="flex items-center justify-between">
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("dashboard.siteVisits")}</h2>
        <div className="flex items-center gap-4 text-xs text-stone-500 dark:text-stone-400">
          <span className="flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-full bg-grove-600" />
            2025
          </span>
          <span className="flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-full bg-grove-200 dark:bg-grove-800" />
            2024
          </span>
        </div>
      </div>
      <div className="mt-4 h-64">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={visits} barGap={4} margin={{ top: 4, right: 0, bottom: 0, left: -12 }}>
            <CartesianGrid stroke={grid} vertical={false} />
            <XAxis dataKey="month" tickLine={false} axisLine={false} tick={{ fill: muted, fontSize: 12 }} />
            <YAxis
              tickLine={false}
              axisLine={false}
              tick={{ fill: muted, fontSize: 12 }}
              tickFormatter={(v: number) => `${v / 1000}k`}
            />
            <Tooltip
              cursor={{ fill: isDark ? "rgba(255,255,255,0.04)" : "rgba(11,11,11,0.04)" }}
              formatter={(value: number) => formatNumber(value)}
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

function RecentOrdersCard() {
  const { t } = useTranslation(["admin", "data"]);
  const orders = useRecentOrders();
  return (
    <Card>
      <div className="flex items-center justify-between">
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("dashboard.recentOrders")}</h2>
        <button className="text-xs font-semibold text-grove-700 hover:text-grove-800 dark:text-grove-400">
          {t("dashboard.viewAll")}
        </button>
      </div>
      <ul className="mt-4 flex flex-col gap-4">
        {orders.map((order) => (
          <li key={order.id} className="flex items-center gap-3">
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">
                {order.id} · {order.customerName}
              </p>
              <p className="truncate text-xs text-stone-400 dark:text-stone-500">
                {t(`data:categories.${order.categoryKey}.name`)}, {order.time}
                <br />
                {order.city}
              </p>
            </div>
            <div className="flex shrink-0 flex-col items-end gap-1.5">
              <p className="text-sm font-semibold text-stone-800 dark:text-stone-100">
                {formatSomoni(order.amount)} {t("common.somoni")}
              </p>
              <span className={`rounded-full px-2 py-0.5 text-[11px] font-semibold ${STATUS_CLASSES[order.status]}`}>
                {t(`dashboard.orderStatus.${order.status}`)}
              </span>
            </div>
          </li>
        ))}
      </ul>
    </Card>
  );
}

function TopFarmersCard() {
  const { t } = useTranslation("admin");
  const farmers = useTopFarmers();
  return (
    <Card>
      <div className="flex items-center justify-between">
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("dashboard.topFarmers")}</h2>
        <button className="text-xs font-semibold text-grove-700 hover:text-grove-800 dark:text-grove-400">
          {t("dashboard.viewAll")}
        </button>
      </div>
      <ul className="mt-4 flex flex-col gap-4">
        {farmers.map((farmer) => (
          <li key={farmer.id} className="flex items-center gap-3">
            <Avatar name={farmer.name} src={farmer.photo} size={40} />
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{farmer.name}</p>
              <p className="truncate text-xs text-stone-400 dark:text-stone-500">{farmer.region}</p>
            </div>
            <div className="flex shrink-0 flex-col items-end gap-1">
              <span className="flex items-center gap-1 text-xs font-semibold text-stone-700 dark:text-stone-200">
                <Star size={12} className="fill-harvest-400 text-harvest-400" />
                {farmer.rating}
              </span>
              <p className="text-xs text-stone-400 dark:text-stone-500">
                {formatNumber(farmer.revenue)} {t("common.somoni")}
              </p>
            </div>
          </li>
        ))}
      </ul>
    </Card>
  );
}

function PopularCategoriesCard() {
  const { t } = useTranslation("admin");
  const categories = usePopularCategories();
  return (
    <Card>
      <div className="flex items-center justify-between">
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("dashboard.popularCategories")}</h2>
        <button className="text-xs font-semibold text-grove-700 hover:text-grove-800 dark:text-grove-400">
          {t("dashboard.viewAll")}
        </button>
      </div>
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
    </Card>
  );
}

export function AdminDashboard() {
  const stats = useAdminStats();

  return (
    <div className="flex flex-col gap-6">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {stats.map((stat) => (
          <StatCard key={stat.key} stat={stat} />
        ))}
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1fr_1.3fr]">
        <RegionDonutCard />
        <SiteVisitsCard />
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <RecentOrdersCard />
        <TopFarmersCard />
        <PopularCategoriesCard />
      </div>
    </div>
  );
}
