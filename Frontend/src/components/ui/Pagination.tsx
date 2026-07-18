import { ChevronLeft, ChevronRight, MoreHorizontal } from "lucide-react";
import { cn } from "@/lib/utils";

function getPageList(current: number, total: number): (number | "dots")[] {
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
  const pages = new Set<number>([1, 2, total - 1, total, current - 1, current, current + 1]);
  const sorted = [...pages].filter((p) => p >= 1 && p <= total).sort((a, b) => a - b);
  const result: (number | "dots")[] = [];
  let prev = 0;
  for (const p of sorted) {
    if (prev && p - prev > 1) result.push("dots");
    result.push(p);
    prev = p;
  }
  return result;
}

export function Pagination({
  page,
  totalPages,
  onPageChange,
  className,
}: {
  page: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  className?: string;
}) {
  if (totalPages <= 1) return null;
  const pages = getPageList(page, totalPages);

  return (
    <nav className={cn("flex items-center justify-center gap-1.5", className)} aria-label="Пагинация">
      <button
        onClick={() => onPageChange(page - 1)}
        disabled={page === 1}
        className="flex h-10 w-10 items-center justify-center rounded-xl border border-stone-200 text-stone-600 transition hover:border-grove-400 hover:text-grove-700 disabled:pointer-events-none disabled:opacity-40 dark:border-stone-700 dark:text-stone-300 dark:hover:border-grove-500 dark:hover:text-grove-400"
        aria-label="Предыдущая страница"
      >
        <ChevronLeft size={18} />
      </button>

      {pages.map((p, i) =>
        p === "dots" ? (
          <span key={`dots-${i}`} className="flex h-10 w-10 items-center justify-center text-stone-400 dark:text-stone-500">
            <MoreHorizontal size={16} />
          </span>
        ) : (
          <button
            key={p}
            onClick={() => onPageChange(p)}
            className={cn(
              "flex h-10 w-10 items-center justify-center rounded-xl text-sm font-medium transition",
              p === page
                ? "bg-grove-700 text-white shadow-(--shadow-glow-grove)"
                : "text-stone-600 hover:bg-stone-100 dark:text-stone-300 dark:hover:bg-stone-800",
            )}
            aria-current={p === page ? "page" : undefined}
          >
            {p}
          </button>
        ),
      )}

      <button
        onClick={() => onPageChange(page + 1)}
        disabled={page === totalPages}
        className="flex h-10 w-10 items-center justify-center rounded-xl border border-stone-200 text-stone-600 transition hover:border-grove-400 hover:text-grove-700 disabled:pointer-events-none disabled:opacity-40 dark:border-stone-700 dark:text-stone-300 dark:hover:border-grove-500 dark:hover:text-grove-400"
        aria-label="Следующая страница"
      >
        <ChevronRight size={18} />
      </button>
    </nav>
  );
}
