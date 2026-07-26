import { useTranslation } from "react-i18next";
import { Sprout, ShoppingBag, Users, Wallet } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { computeMonthlyTrend, formatNumber } from "@/lib/utils";
import { OrderStatus } from "@/lib/orderStatus";
import type { AdminAnalyticsDto, AdminOrderDto, AdminUserDto } from "@/data/adminEntities";
import type { Category, Farmer } from "@/types";

export interface AdminStat {
  key: "revenue" | "customers" | "orders" | "farmers";
  value: number;
  suffix?: string;
  changePercent: number | null;
  compareValue: string;
  icon: LucideIcon;
  accent: "grove" | "blue" | "orange" | "rose";
  trend: number[];
}

export interface RegionSales {
  key: string;
  name: string;
  amount: number;
  percent: number;
}

export interface SiteVisitPoint {
  month: string;
  current: number;
  previous: number;
}

export interface RecentOrder {
  orderNumber: string;
  customerId: number;
  region: string;
  district: string;
  amount: number;
  status: number;
  createdAt: string;
}

export interface TopFarmerRow {
  id: number;
  name: string;
  region: string;
  rating: number;
  revenue: number;
  avatarUrl?: string;
}

export interface PopularCategoryRow {
  slug: string;
  name: string;
  icon: LucideIcon;
  percent: number;
}

const REGION_COLORS_LIGHT = ["#2a78d6", "#008300", "#e87ba4", "#eda100", "#4a3aa7"];
const REGION_COLORS_DARK = ["#3987e5", "#008300", "#d55181", "#c98500", "#7c3aed"];

export function getRegionColor(index: number, isDark: boolean) {
  const palette = isDark ? REGION_COLORS_DARK : REGION_COLORS_LIGHT;
  return palette[index % palette.length];
}

// Последние N календарных месяцев, заканчивая текущим — тот же принцип
// окна, что и у бэкенда в AnalyticsRepository.GetRevenueByMonthAsync
// (раздел 10.4 ТЗ), только считается на фронте для метрик, для которых
// бэкенд не готовит помесячную выборку (заказы/новые покупатели/фермеры).
function monthsWindow(count: number): { year: number; month: number }[] {
  const now = new Date();
  const result: { year: number; month: number }[] = [];
  for (let i = count - 1; i >= 0; i--) {
    const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
    result.push({ year: d.getFullYear(), month: d.getMonth() + 1 });
  }
  return result;
}

function countByMonth(dates: string[], count: number) {
  const parsed = dates.map((d) => new Date(d));
  return monthsWindow(count).map(({ year, month }) => ({
    year,
    month,
    revenue: parsed.filter((d) => d.getFullYear() === year && d.getMonth() + 1 === month).length,
  }));
}

function cumulativeCountByMonth(dates: string[], count: number) {
  const parsed = dates.map((d) => new Date(d));
  return monthsWindow(count).map(({ year, month }) => {
    const monthEnd = new Date(year, month, 1);
    return { year, month, revenue: parsed.filter((d) => d < monthEnd).length };
  });
}

// Единственный источник помесячной выручки, который реально считает бэкенд
// (revenueByMonth в /analytics/admin/dashboard, только Completed-заказы —
// раздел 10.4 ТЗ) — для заказов/покупателей/фермеров такой готовой выборки
// нет, поэтому считаем её на фронте из уже загруженных сырых списков (тот же
// приём "грузим всё, агрегируем на фронте", что и везде в проекте).
export function useAdminStats(analytics: AdminAnalyticsDto, orders: AdminOrderDto[], customers: AdminUserDto[], farmers: Farmer[]): AdminStat[] {
  const { t } = useTranslation("admin");

  const revenueSeries = [...analytics.revenueByMonth].sort((a, b) => a.year - b.year || a.month - b.month);
  const revenueTrend = computeMonthlyTrend(analytics.revenueByMonth);
  const prevRevenue = revenueSeries.length >= 2 ? revenueSeries[revenueSeries.length - 2].revenue : 0;

  const ordersMonthly = countByMonth(orders.map((o) => o.createdAt), 6);
  const ordersTrend = computeMonthlyTrend(ordersMonthly);
  const prevOrders = ordersMonthly.length >= 2 ? ordersMonthly[ordersMonthly.length - 2].revenue : 0;

  const customersMonthly = countByMonth(customers.map((c) => c.createdAt), 6);
  const customersTrend = computeMonthlyTrend(customersMonthly);
  const prevCustomers = customersMonthly.length >= 2 ? customersMonthly[customersMonthly.length - 2].revenue : 0;

  const farmersCumulative = cumulativeCountByMonth(farmers.map((f) => f.joinedAt), 6);
  const farmersTrend = computeMonthlyTrend(farmersCumulative);
  const prevFarmers = farmersCumulative.length >= 2 ? farmersCumulative[farmersCumulative.length - 2].revenue : 0;

  return [
    {
      key: "revenue",
      value: analytics.revenueThisMonth,
      suffix: t("common.somoni"),
      changePercent: revenueTrend.changePercent,
      compareValue: formatNumber(prevRevenue),
      icon: Wallet,
      accent: "grove",
      trend: revenueTrend.sparkline,
    },
    {
      key: "customers",
      value: customersMonthly.at(-1)?.revenue ?? 0,
      changePercent: customersTrend.changePercent,
      compareValue: formatNumber(prevCustomers),
      icon: Users,
      accent: "blue",
      trend: customersTrend.sparkline,
    },
    {
      key: "orders",
      value: analytics.ordersThisMonth,
      changePercent: ordersTrend.changePercent,
      compareValue: formatNumber(prevOrders),
      icon: ShoppingBag,
      accent: "orange",
      trend: ordersTrend.sparkline,
    },
    {
      key: "farmers",
      value: farmers.length,
      changePercent: farmersTrend.changePercent,
      compareValue: formatNumber(prevFarmers),
      icon: Sprout,
      accent: "rose",
      trend: farmersTrend.sparkline,
    },
  ];
}

// Раздел 10.4 ТЗ: "выручка" = сумма Completed-заказов. Регион берётся из
// Order.Region — это свободный текст, введённый покупателем при оформлении
// (см. Checkout.tsx), поэтому список регионов складывается из того, что
// реально встретилось в заказах, а не из фиксированного списка областей.
export function computeRegionSales(orders: AdminOrderDto[]): RegionSales[] {
  const sums = new Map<string, number>();
  for (const o of orders) {
    if (o.status !== OrderStatus.Completed) continue;
    sums.set(o.region, (sums.get(o.region) ?? 0) + o.totalAmount);
  }
  const total = [...sums.values()].reduce((a, b) => a + b, 0);
  return [...sums.entries()]
    .sort((a, b) => b[1] - a[1])
    .map(([name, amount]) => ({
      key: name,
      name,
      amount,
      percent: total > 0 ? Math.round((amount / total) * 1000) / 10 : 0,
    }));
}

// Честная замена "посещений сайта" — в проекте нет трекинга трафика, но есть
// реальные заказы с датой создания, так что вместо выдуманных цифр показываем
// такое же по форме (месяц-к-месяцу, текущий год/прошлый год) сравнение по
// количеству заказов.
export function useOrdersByMonth(orders: AdminOrderDto[]): SiteVisitPoint[] {
  const { t } = useTranslation("admin");
  const monthNames = t("statistics.months", { returnObjects: true }) as string[];
  const dates = orders.map((o) => new Date(o.createdAt));

  return monthsWindow(6).map(({ year, month }) => {
    const current = dates.filter((d) => d.getFullYear() === year && d.getMonth() + 1 === month).length;
    const previous = dates.filter((d) => d.getFullYear() === year - 1 && d.getMonth() + 1 === month).length;
    return { month: monthNames[month - 1] ?? String(month), current, previous };
  });
}

export function computeRecentOrders(orders: AdminOrderDto[]): RecentOrder[] {
  return orders.slice(0, 4).map((o) => ({
    orderNumber: o.orderNumber,
    customerId: o.customerId,
    region: o.region,
    district: o.district,
    amount: o.totalAmount,
    status: o.status,
    createdAt: o.createdAt,
  }));
}

// Топ фермеры по реальной выручке (сумма Completed-заказов по farmerId) —
// имя/регион/рейтинг уже реальные из useFarmers() (реальные отзывы, раздел
// 8.13 ТЗ), выручку бэкенд по фермеру массово не отдаёт (только для самого
// фермера через /analytics/farmer/dashboard), поэтому считаем на фронте.
export function computeTopFarmers(orders: AdminOrderDto[], farmers: Farmer[]): TopFarmerRow[] {
  const revenueByFarmerId = new Map<number, number>();
  for (const o of orders) {
    if (o.status !== OrderStatus.Completed) continue;
    revenueByFarmerId.set(o.farmerId, (revenueByFarmerId.get(o.farmerId) ?? 0) + o.totalAmount);
  }
  return farmers
    .map((f) => ({
      id: f.id,
      name: f.farmName,
      region: f.region,
      rating: f.rating,
      revenue: revenueByFarmerId.get(f.id) ?? 0,
      avatarUrl: f.avatarUrl,
    }))
    .sort((a, b) => b.revenue - a.revenue || b.rating - a.rating)
    .slice(0, 4);
}

// Популярность категории — доля активных объявлений этой категории от всех
// активных объявлений (то же реальное productCount, что уже используется в
// каталоге, data/catalogStore.ts) — честная замена выдуманной доли выручки.
export function computePopularCategories(categories: Category[]): PopularCategoryRow[] {
  const total = categories.reduce((sum, c) => sum + c.productCount, 0);
  return categories
    .filter((c) => c.productCount > 0)
    .sort((a, b) => b.productCount - a.productCount)
    .slice(0, 5)
    .map((c) => ({
      slug: c.slug,
      name: c.name,
      icon: c.icon,
      percent: total > 0 ? Math.round((c.productCount / total) * 1000) / 10 : 0,
    }));
}
