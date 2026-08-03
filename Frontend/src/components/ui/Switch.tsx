import { cn } from "@/lib/utils";

interface SwitchProps {
  checked: boolean;
  onChange: () => void;
  disabled?: boolean;
  size?: "sm" | "md";
  "aria-label"?: string;
}

// Полноценный ползунок вкл/выкл (не чекбокс — checkbox в Field.tsx остаётся
// для форм, где важна нейтральная семантика "отметить пункт"). Здесь
// специально нужен визуально заметный переключатель — например, статус
// курьера "доступен для заказов" должен читаться с одного взгляда, а не
// теряться среди прочих полей формы.
export function Switch({ checked, onChange, disabled, size = "md", ...rest }: SwitchProps) {
  const trackSize = size === "sm" ? "h-6 w-11" : "h-7 w-13";
  const dotSize = size === "sm" ? "h-4.5 w-4.5" : "h-5.5 w-5.5";
  const translate = size === "sm" ? (checked ? "translate-x-5.5" : "translate-x-0.5") : checked ? "translate-x-6.5" : "translate-x-0.5";

  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      disabled={disabled}
      onClick={onChange}
      className={cn(
        "relative shrink-0 rounded-full transition-colors duration-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-grove-300 disabled:cursor-not-allowed disabled:opacity-60",
        trackSize,
        checked ? "bg-grove-600 dark:bg-grove-500" : "bg-stone-200 dark:bg-stone-700",
      )}
      {...rest}
    >
      <span
        className={cn(
          "absolute top-0.5 left-0 inline-block rounded-full bg-white shadow-sm transition-transform duration-200",
          dotSize,
          translate,
        )}
      />
    </button>
  );
}
