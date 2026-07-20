import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ChevronRight, Home } from "lucide-react";
import { cn } from "@/lib/utils";

export interface Crumb {
  label: string;
  to?: string;
}

export function Breadcrumbs({ items, className }: { items: Crumb[]; className?: string }) {
  const { t } = useTranslation("ui");
  return (
    <nav className={cn("flex items-center gap-1.5 text-sm", className)} aria-label={t("breadcrumbs")}>
      <Link to="/" className="flex items-center text-stone-400 transition hover:text-grove-700 dark:text-stone-500 dark:hover:text-grove-400">
        <Home size={14} />
      </Link>
      {items.map((item, i) => (
        <span key={i} className="flex items-center gap-1.5">
          <ChevronRight size={13} className="text-stone-300 dark:text-stone-600" />
          {item.to ? (
            <Link to={item.to} className="text-stone-500 transition hover:text-grove-700 dark:text-stone-400 dark:hover:text-grove-400">
              {item.label}
            </Link>
          ) : (
            <span className="font-medium text-stone-800 dark:text-stone-200">{item.label}</span>
          )}
        </span>
      ))}
    </nav>
  );
}
