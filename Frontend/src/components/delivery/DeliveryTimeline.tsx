import { useTranslation } from "react-i18next";
import { Check } from "lucide-react";
import { cn } from "@/lib/utils";
import { DeliveryStatus } from "@/data/delivery";

// 6 крупных шагов для покупателя/фермера — по прямому запросу пользователя.
// Более гранулярные статусы курьера (Accepted/ArrivedAtFarmer/ArrivedAtClient)
// сворачиваются в соседний крупный шаг, чтобы не перегружать таймлайн.
const STEPS = [
  { key: "orderConfirmed", threshold: 0 },
  { key: "courierAssigned", threshold: DeliveryStatus.Assigned },
  { key: "goingToFarmer", threshold: DeliveryStatus.GoingToFarmer },
  { key: "pickedUp", threshold: DeliveryStatus.PickedUp },
  { key: "onTheWay", threshold: DeliveryStatus.InTransit },
  { key: "delivered", threshold: DeliveryStatus.Delivered },
] as const;

export function DeliveryTimeline({ orderConfirmed, status }: { orderConfirmed: boolean; status: number | null }) {
  const { t } = useTranslation("delivery");

  // Индекс текущего активного шага: 0, если заказ ещё не подтверждён;
  // иначе — самый дальний шаг, порог которого достигнут статусом доставки.
  const currentIndex = !orderConfirmed
    ? 0
    : STEPS.reduce((acc, step, i) => ((status ?? 0) >= step.threshold ? i : acc), 0);

  return (
    <div className="flex flex-col gap-0">
      {STEPS.map((step, i) => {
        const done = i < currentIndex || (i === currentIndex && status === DeliveryStatus.Delivered);
        const active = i === currentIndex && !done;
        const isLast = i === STEPS.length - 1;
        return (
          <div key={step.key} className="flex gap-3">
            <div className="flex flex-col items-center">
              <span
                className={cn(
                  "flex h-6 w-6 shrink-0 items-center justify-center rounded-full border-2 text-[11px] font-bold transition-colors",
                  done
                    ? "border-grove-600 bg-grove-600 text-white"
                    : active
                      ? "border-grove-600 bg-white text-grove-700 dark:bg-stone-900"
                      : "border-stone-200 bg-white text-stone-300 dark:border-stone-700 dark:bg-stone-900 dark:text-stone-600",
                )}
              >
                {done ? <Check size={13} /> : i + 1}
              </span>
              {!isLast && (
                <span className={cn("my-0.5 w-0.5 flex-1 min-h-6", done ? "bg-grove-600" : "bg-stone-200 dark:bg-stone-700")} />
              )}
            </div>
            <p
              className={cn(
                "pb-6 text-sm leading-6",
                active
                  ? "font-semibold text-stone-900 dark:text-stone-50"
                  : done
                    ? "text-stone-600 dark:text-stone-400"
                    : "text-stone-400 dark:text-stone-500",
              )}
            >
              {t(`timeline.${step.key}`)}
            </p>
          </div>
        );
      })}
    </div>
  );
}
