import { Star } from "lucide-react";
import { cn } from "@/lib/utils";

export function RatingStars({
  rating,
  size = 14,
  showValue = false,
  reviewCount,
  className,
}: {
  rating: number;
  size?: number;
  showValue?: boolean;
  reviewCount?: number;
  className?: string;
}) {
  return (
    <div className={cn("inline-flex items-center gap-1", className)}>
      <div className="flex items-center gap-0.5">
        {Array.from({ length: 5 }).map((_, i) => {
          const filled = rating >= i + 1;
          const half = !filled && rating > i && rating < i + 1;
          return (
            <span key={i} className="relative inline-block" style={{ width: size, height: size }}>
              <Star size={size} className="absolute inset-0 text-stone-200 dark:text-stone-700" fill="currentColor" />
              {(filled || half) && (
                <span
                  className="absolute inset-0 overflow-hidden"
                  style={{ width: half ? "50%" : "100%" }}
                >
                  <Star size={size} className="text-harvest-500" fill="currentColor" />
                </span>
              )}
            </span>
          );
        })}
      </div>
      {showValue && <span className="text-sm font-semibold text-stone-800 dark:text-stone-200">{rating.toFixed(1)}</span>}
      {reviewCount !== undefined && (
        <span className="text-sm text-stone-500 dark:text-stone-400">({reviewCount})</span>
      )}
    </div>
  );
}
