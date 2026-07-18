import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

const VARIANTS = {
  grove: "bg-grove-100 text-grove-800 dark:bg-grove-900 dark:text-grove-200",
  harvest: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  clay: "bg-clay-100 text-clay-600 dark:bg-clay-500/25 dark:text-clay-400",
  stone: "bg-stone-100 text-stone-700 dark:bg-stone-800 dark:text-stone-200",
  dark: "bg-stone-900 text-white dark:bg-stone-100 dark:text-stone-900",
  outline: "border border-stone-300 text-stone-700 bg-white/60 dark:border-stone-600 dark:bg-stone-900/60 dark:text-stone-200",
  success: "bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-100",
  danger: "bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-100",
} as const;

export function Badge({
  children,
  variant = "grove",
  className,
  icon,
}: {
  children: ReactNode;
  variant?: keyof typeof VARIANTS;
  className?: string;
  icon?: ReactNode;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-semibold tracking-wide",
        VARIANTS[variant],
        className,
      )}
    >
      {icon}
      {children}
    </span>
  );
}
