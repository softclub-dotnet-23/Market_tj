import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

// Раньше карточки в Admin/Farmer/Customer-панелях были голыми `div` без тени
// (просто border) — плоско на фоне остального сайта, где у карточек уже
// есть --shadow-soft/--shadow-card (см. ProductCard/FarmerCard и т.д.).
// Этот компонент подтягивает панели к той же визуальной системе.
export function Card({ className, children }: { className?: string; children: ReactNode }) {
  return (
    <div
      className={cn(
        "rounded-3xl border border-stone-100 bg-white p-6 shadow-(--shadow-soft) transition-shadow duration-300 hover:shadow-(--shadow-card) dark:border-stone-800 dark:bg-stone-900",
        className,
      )}
    >
      {children}
    </div>
  );
}
