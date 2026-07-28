import { LayoutGrid, List } from "lucide-react";
import { useTranslation } from "react-i18next";
import { cn } from "@/lib/utils";

export type OrdersViewMode = "table" | "cards";

// Переключатель "Список / Карточки" над таблицей заказов — видим только на
// lg+ (на телефоне и так всегда карточки, переключать нечего, см. страницы
// заказов). Пункт меню сохраняется в состоянии страницы, не персистится —
// каждый заход начинается со списка.
export function ViewModeToggle({ value, onChange, ns }: { value: OrdersViewMode; onChange: (v: OrdersViewMode) => void; ns: string }) {
  const { t } = useTranslation(ns);
  return (
    <div className="hidden items-center gap-1 rounded-xl bg-stone-100 p-1 lg:inline-flex dark:bg-stone-800">
      <button
        onClick={() => onChange("table")}
        className={cn(
          "flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm font-medium transition",
          value === "table"
            ? "bg-white text-stone-900 shadow-(--shadow-soft) dark:bg-stone-700 dark:text-stone-50"
            : "text-stone-500 hover:text-stone-700 dark:text-stone-400 dark:hover:text-stone-200",
        )}
      >
        <List size={14} />
        {t("orders.viewList")}
      </button>
      <button
        onClick={() => onChange("cards")}
        className={cn(
          "flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm font-medium transition",
          value === "cards"
            ? "bg-white text-stone-900 shadow-(--shadow-soft) dark:bg-stone-700 dark:text-stone-50"
            : "text-stone-500 hover:text-stone-700 dark:text-stone-400 dark:hover:text-stone-200",
        )}
      >
        <LayoutGrid size={14} />
        {t("orders.viewCards")}
      </button>
    </div>
  );
}
