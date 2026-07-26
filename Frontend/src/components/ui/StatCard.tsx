import { useId } from "react";
import type { LucideIcon } from "lucide-react";
import { ArrowDownRight, ArrowUpRight, Minus } from "lucide-react";
import { Area, AreaChart, ResponsiveContainer } from "recharts";
import { Card } from "@/components/ui/Card";
import { cn } from "@/lib/utils";

const ACCENTS = {
  grove: { chip: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300", line: "#298a47" },
  blue: { chip: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300", line: "#2a78d6" },
  orange: { chip: "bg-orange-100 text-orange-700 dark:bg-orange-900 dark:text-orange-300", line: "#ea7a1f" },
  rose: { chip: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300", line: "#e0507a" },
} as const;

// changePercent: null — нет базы для сравнения (например только 1 месяц
// данных). sparkline — оставь undefined, если для метрики нет истории
// (у большинства счётчиков в AdminAnalyticsDto её просто нет) — trend
// вообще не передавай в таком случае, а не подставляй выдуманные числа.
export interface StatCardTrend {
  changePercent: number | null;
  sparkline?: number[];
}

interface StatCardProps {
  icon: LucideIcon;
  accent: keyof typeof ACCENTS;
  label: string;
  value: string;
  trend?: StatCardTrend;
  compareLabel?: string;
  className?: string;
}

export function StatCard({ icon: Icon, accent, label, value, trend, compareLabel, className }: StatCardProps) {
  const gradientId = useId();
  const { chip, line } = ACCENTS[accent];
  const positive = trend?.changePercent != null && trend.changePercent > 0;
  const negative = trend?.changePercent != null && trend.changePercent < 0;

  return (
    <Card className={cn("flex flex-col gap-3", className)}>
      <div className="flex items-center gap-3">
        <span className={cn("flex h-11 w-11 shrink-0 items-center justify-center rounded-full", chip)}>
          <Icon size={19} />
        </span>
        <p className="min-w-0 truncate text-sm text-stone-500 dark:text-stone-400">{label}</p>
      </div>
      <p className="font-display text-2xl text-stone-900 dark:text-stone-50">{value}</p>

      {trend && (
        <div className="flex items-center justify-between gap-3 border-t border-stone-100 pt-3 dark:border-stone-800">
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
            {trend.changePercent != null ? `${Math.abs(trend.changePercent).toFixed(0)}%` : "—"}
            {compareLabel && <span className="ml-1 font-normal text-stone-400 dark:text-stone-500">{compareLabel}</span>}
          </span>

          {trend.sparkline && trend.sparkline.length >= 2 && (
            <div className="h-8 w-16 shrink-0">
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={trend.sparkline.map((v, i) => ({ i, v }))} margin={{ top: 2, right: 0, bottom: 0, left: 0 }}>
                  <defs>
                    <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
                      <stop offset="0%" stopColor={line} stopOpacity={0.35} />
                      <stop offset="100%" stopColor={line} stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <Area type="monotone" dataKey="v" stroke={line} strokeWidth={2} fill={`url(#${gradientId})`} />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          )}
        </div>
      )}
    </Card>
  );
}
