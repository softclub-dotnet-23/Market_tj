import { useTranslation } from "react-i18next";
import { Wallet, Users, ShoppingBag, Sprout } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { useFarmers } from "@/data/farmers";
import { useCategories } from "@/data/categories";
import { farmerPhotos } from "@/assets/photos";

export type OrderStatus = "new" | "processing" | "shipped" | "delivered";

export interface AdminStat {
  key: "revenue" | "customers" | "orders" | "farmers";
  value: number;
  suffix?: string;
  changePercent: number;
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
  color: string;
}

export interface SiteVisitPoint {
  month: string;
  current: number;
  previous: number;
}

export interface RecentOrder {
  id: string;
  customerName: string;
  categoryKey: string;
  city: string;
  time: string;
  amount: number;
  status: OrderStatus;
}

export interface TopFarmerRow {
  id: number;
  name: string;
  region: string;
  rating: number;
  revenue: number;
  photo?: string;
}

export interface PopularCategoryRow {
  slug: string;
  name: string;
  icon: LucideIcon;
  amount: number;
  percent: number;
}

const REGION_COLORS_LIGHT = ["#2a78d6", "#008300", "#e87ba4", "#eda100", "#4a3aa7"];
const REGION_COLORS_DARK = ["#3987e5", "#008300", "#d55181", "#c98500", "#7c3aed"];

export function useAdminStats(): AdminStat[] {
  const { t } = useTranslation("admin");
  return [
    {
      key: "revenue",
      value: 124850,
      suffix: t("common.somoni"),
      changePercent: 18.6,
      compareValue: "105 420",
      icon: Wallet,
      accent: "grove",
      trend: [58, 62, 60, 68, 72, 78, 90, 100],
    },
    {
      key: "customers",
      value: 1247,
      changePercent: 24.3,
      compareValue: "1 003",
      icon: Users,
      accent: "blue",
      trend: [40, 46, 44, 52, 58, 64, 70, 82],
    },
    {
      key: "orders",
      value: 2358,
      changePercent: 15.2,
      compareValue: "2 047",
      icon: ShoppingBag,
      accent: "orange",
      trend: [55, 58, 54, 60, 66, 68, 74, 84],
    },
    {
      key: "farmers",
      value: 482,
      changePercent: 9.8,
      compareValue: "439",
      icon: Sprout,
      accent: "rose",
      trend: [70, 72, 71, 75, 78, 80, 85, 92],
    },
  ];
}

export function useRegionSales(): RegionSales[] {
  const { t } = useTranslation("admin");
  const amounts = [42700, 30860, 23100, 15740, 12450];
  const keys = ["dushanbe", "khatlon", "sughd", "gbao", "rrp"];
  const total = amounts.reduce((a, b) => a + b, 0);
  return keys.map((key, i) => ({
    key,
    name: t(`dashboard.regions.${key}`),
    amount: amounts[i],
    percent: Math.round((amounts[i] / total) * 1000) / 10,
    color: REGION_COLORS_LIGHT[i],
  }));
}

export function getRegionColor(index: number, isDark: boolean) {
  return (isDark ? REGION_COLORS_DARK : REGION_COLORS_LIGHT)[index];
}

export function useSiteVisits(): SiteVisitPoint[] {
  const { t } = useTranslation("admin");
  const previous = [32000, 28000, 35000, 40000, 38000, 36000];
  const current = [42000, 36000, 52000, 58000, 62000, 55000];
  const months = t("dashboard.months", { returnObjects: true }) as string[];
  return months.map((month, i) => ({ month, current: current[i], previous: previous[i] }));
}

export function useRecentOrders(): RecentOrder[] {
  return [
    { id: "MT-154672", customerName: "Файзали Н.", categoryKey: "vegetables", city: "Душанбе", time: "10:12", amount: 320, status: "new" },
    { id: "MT-154671", customerName: "Мирзоев А.", categoryKey: "vegetables", city: "Душанбе", time: "09:15", amount: 185, status: "processing" },
    { id: "MT-154670", customerName: "Сайдова Ю.", categoryKey: "fruits", city: "Бохтар", time: "08:21", amount: 95, status: "shipped" },
    { id: "MT-154669", customerName: "Негмат Т.", categoryKey: "vegetables", city: "Бохтар", time: "07:33", amount: 275, status: "delivered" },
  ];
}

const TOP_FARMER_IDS = [2, 3, 5, 9];
const TOP_FARMER_REVENUE: Record<number, number> = { 2: 12450, 3: 9870, 5: 8230, 9: 7640 };

export function useTopFarmers(): TopFarmerRow[] {
  const farmers = useFarmers();
  return TOP_FARMER_IDS.map((id) => {
    const farmer = farmers.find((f) => f.id === id)!;
    return {
      id,
      name: farmer.ownerName,
      region: farmer.region,
      rating: farmer.rating,
      revenue: TOP_FARMER_REVENUE[id],
      photo: farmerPhotos[id],
    };
  }).sort((a, b) => b.revenue - a.revenue);
}

export function usePopularCategories(): PopularCategoryRow[] {
  const categories = useCategories();
  const amounts: Record<string, number> = {
    vegetables: 42560,
    fruits: 31240,
    dairy: 16300,
    "dried-fruits": 15780,
    nuts: 12760,
  };
  const total = Object.values(amounts).reduce((a, b) => a + b, 0);
  return categories
    .filter((c) => amounts[c.slug] !== undefined)
    .map((c) => ({
      slug: c.slug,
      name: c.name,
      icon: c.icon,
      amount: amounts[c.slug],
      percent: Math.round((amounts[c.slug] / total) * 1000) / 10,
    }))
    .sort((a, b) => b.amount - a.amount);
}
