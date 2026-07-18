import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

export function Chip({
  children,
  active = false,
  onClick,
  icon,
  onRemove,
  className,
}: {
  children: ReactNode;
  active?: boolean;
  onClick?: () => void;
  icon?: ReactNode;
  onRemove?: () => void;
  className?: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "inline-flex items-center gap-1.5 whitespace-nowrap rounded-full border px-3.5 py-2 text-sm font-medium transition-all",
        active
          ? "border-grove-700 bg-grove-700 text-white dark:border-grove-600 dark:bg-grove-600"
          : "border-stone-200 bg-white text-stone-600 hover:border-stone-300 hover:bg-stone-50 dark:border-stone-700 dark:bg-stone-900 dark:text-stone-300 dark:hover:border-stone-600 dark:hover:bg-stone-800",
        className,
      )}
    >
      {icon}
      {children}
      {onRemove && (
        <span
          onClick={(e) => {
            e.stopPropagation();
            onRemove();
          }}
          className="ml-0.5 opacity-70 hover:opacity-100"
        >
          ×
        </span>
      )}
    </button>
  );
}
