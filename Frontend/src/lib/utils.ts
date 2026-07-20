import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";
import i18n from "@/lib/i18n";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

function numberLocale() {
  return i18n.resolvedLanguage === "tj" ? "tg-TJ" : "ru-RU";
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

export function formatDate(iso: string) {
  const d = new Date(iso);
  const months = i18n.t("dates.months", { ns: "common", returnObjects: true }) as string[];
  return `${d.getDate()} ${months[d.getMonth()]} ${d.getFullYear()}`;
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
