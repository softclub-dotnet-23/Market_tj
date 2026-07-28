import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";
import i18n from "@/lib/i18n";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

function numberLocale() {
  switch (i18n.resolvedLanguage) {
    case "tj":
      return "tg-TJ";
    case "en":
      return "en-US";
    default:
      return "ru-RU";
  }
}

export function formatSomoni(amount: number) {
  return new Intl.NumberFormat(numberLocale(), {
    maximumFractionDigits: amount % 1 === 0 ? 0 : 2,
    minimumFractionDigits: 0,
  }).format(amount);
}

export function formatNumber(amount: number) {
  return new Intl.NumberFormat(numberLocale()).format(amount);
}

// Единственное место, где решается, розничная цена применяется или оптовая —
// используется везде, где считается сумма по товару (корзина, мини-корзина,
// страница товара, чекаут), чтобы не разъезжались между собой: раньше цена
// заказа везде считалась только по retailPricePerKg, из-за чего порог
// "от N кг — оптовая цена" был просто текстом на странице и на сумму не влиял.
export function getUnitPrice(
  product: { retailPricePerKg: number; wholesalePricePerKg?: number; wholesaleMinimumQuantity?: number },
  quantity: number,
): number {
  if (
    product.wholesalePricePerKg != null &&
    product.wholesaleMinimumQuantity != null &&
    quantity >= product.wholesaleMinimumQuantity
  ) {
    return product.wholesalePricePerKg;
  }
  return product.retailPricePerKg;
}

export function formatDate(iso: string) {
  const d = new Date(iso);
  const months = i18n.t("dates.months", { ns: "common", returnObjects: true }) as string[];
  return `${d.getDate()} ${months[d.getMonth()]} ${d.getFullYear()}`;
}

// Компактный числовой формат (не "27 июля 2026, 06:31") — специально короче
// formatDate: используется в плотных таблицах заказов и в чате, где
// многословный формат с названием месяца переносился на 2-3 строки и раздувал
// высоту строк/пузырей сообщений.
export function formatDateTime(iso: string) {
  const d = new Date(iso);
  const day = String(d.getDate()).padStart(2, "0");
  const month = String(d.getMonth() + 1).padStart(2, "0");
  const hours = String(d.getHours()).padStart(2, "0");
  const minutes = String(d.getMinutes()).padStart(2, "0");
  return `${day}.${month}.${d.getFullYear()}, ${hours}:${minutes}`;
}

// Только время (без даты) — для пузырей сообщений в чате, где дата и так
// показана один раз отдельным разделителем над группой сообщений одного дня
// (см. ChatModal.tsx), повторять её в каждом пузыре избыточно.
export function formatTime(iso: string) {
  const d = new Date(iso);
  const hours = String(d.getHours()).padStart(2, "0");
  const minutes = String(d.getMinutes()).padStart(2, "0");
  return `${hours}:${minutes}`;
}

// Разделитель дат в чате — "Сегодня"/"Вчера" или полная дата, по календарному
// дню (а не разнице в часах, как у timeAgo — иначе сообщение в 23:59 вчера
// могло бы ошибочно показаться "сегодня", если сейчас 00:05).
export function formatChatDate(iso: string) {
  const d = new Date(iso);
  const now = new Date();
  const startOfDay = (date: Date) => new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime();
  const diffDays = Math.round((startOfDay(now) - startOfDay(d)) / 86_400_000);
  if (diffDays === 0) return i18n.t("dates.today", { ns: "common" });
  if (diffDays === 1) return i18n.t("dates.yesterday", { ns: "common" });
  return formatDate(iso);
}

export function timeAgo(iso: string) {
  const diffMs = Date.now() - new Date(iso).getTime();
  const day = 24 * 60 * 60 * 1000;
  const days = Math.floor(diffMs / day);
  if (days <= 0) return i18n.t("dates.today", { ns: "common" });
  if (days === 1) return i18n.t("dates.yesterday", { ns: "common" });
  if (days < 7) return i18n.t("dates.daysAgo", { ns: "common", count: days });
  if (days < 30) return i18n.t("dates.weeksAgo", { ns: "common", count: Math.floor(days / 7) });
  if (days < 365) return i18n.t("dates.monthsAgo", { ns: "common", count: Math.floor(days / 30) });
  return i18n.t("dates.yearsAgo", { ns: "common", count: Math.floor(days / 365) });
}

// Бэкенд (Npgsql/Postgres `timestamp with time zone`) требует DateTime с
// Kind=Utc — голая дата из <input type="date"> ("2026-07-24") при разборе
// System.Text.Json получает Kind=Unspecified и роняет запрос 500-й ошибкой
// (ArgumentException: "Cannot write DateTime with Kind=Unspecified...").
// Достаточно самим дописать время+Z, не трогая бэкенд.
export function dateInputToIso(value: string): string | null {
  if (!value) return null;
  return `${value}T00:00:00.000Z`;
}

export function daysAgoISO(days: number) {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return d.toISOString();
}

export function clamp(value: number, min: number, max: number) {
  return Math.min(Math.max(value, min), max);
}

export function slugify(value: string) {
  return value
    .toLowerCase()
    .replace(/[^a-zа-я0-9]+/gi, "-")
    .replace(/(^-|-$)/g, "");
}

// Реальный тренд для StatCard из помесячного ряда (AdminAnalyticsDto.revenueByMonth /
// FarmerDashboardDto.revenueByMonth — единственные метрики в проекте, у которых
// вообще есть история). changePercent: null, если сравнивать не с чем (меньше
// двух месяцев данных) — компонент в этом случае покажет "—", а не подделает число.
export function computeMonthlyTrend(entries: { year: number; month: number; revenue: number }[]) {
  const sorted = [...entries].sort((a, b) => a.year - b.year || a.month - b.month);
  const sparkline = sorted.slice(-6).map((e) => e.revenue);

  if (sorted.length < 2) {
    return { changePercent: null as number | null, sparkline };
  }

  const previous = sorted[sorted.length - 2].revenue;
  const current = sorted[sorted.length - 1].revenue;
  const changePercent = previous === 0 ? (current === 0 ? 0 : null) : ((current - previous) / previous) * 100;

  return { changePercent, sparkline };
}
