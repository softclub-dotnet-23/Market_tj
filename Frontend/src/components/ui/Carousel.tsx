import { useCallback, useEffect, useState } from "react";
import type { ReactNode } from "react";
import useEmblaCarousel from "embla-carousel-react";
import { useTranslation } from "react-i18next";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { cn } from "@/lib/utils";

export function Carousel({
  children,
  className,
  slideClassName,
  options,
  showControls = true,
  showDots = false,
}: {
  children: ReactNode[];
  className?: string;
  slideClassName?: string;
  options?: Parameters<typeof useEmblaCarousel>[0];
  showControls?: boolean;
  showDots?: boolean;
}) {
  const { t } = useTranslation("ui");
  const [emblaRef, emblaApi] = useEmblaCarousel({ align: "start", dragFree: false, ...options });
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [canPrev, setCanPrev] = useState(false);
  const [canNext, setCanNext] = useState(false);

  const onSelect = useCallback(() => {
    if (!emblaApi) return;
    setSelectedIndex(emblaApi.selectedScrollSnap());
    setCanPrev(emblaApi.canScrollPrev());
    setCanNext(emblaApi.canScrollNext());
  }, [emblaApi]);

  useEffect(() => {
    if (!emblaApi) return;
    onSelect();
    emblaApi.on("select", onSelect);
    emblaApi.on("reInit", onSelect);
  }, [emblaApi, onSelect]);

  return (
    <div className={cn("relative", className)}>
      <div className="overflow-hidden" ref={emblaRef}>
        <div className="flex gap-5">
          {children.map((child, i) => (
            <div key={i} className={cn("min-w-0 shrink-0 grow-0", slideClassName)}>
              {child}
            </div>
          ))}
        </div>
      </div>

      {showControls && (
        <div className="mt-6 flex items-center justify-center gap-3 sm:absolute sm:-top-20 sm:right-0 sm:mt-0 sm:justify-end">
          <button
            onClick={() => emblaApi?.scrollPrev()}
            disabled={!canPrev}
            aria-label={t("carouselPrev")}
            className="flex h-11 w-11 items-center justify-center rounded-full border border-stone-200 bg-white text-stone-600 transition hover:border-grove-400 hover:text-grove-700 disabled:pointer-events-none disabled:opacity-30 dark:border-stone-700 dark:bg-stone-900 dark:text-stone-300 dark:hover:border-grove-500 dark:hover:text-grove-400"
          >
            <ChevronLeft size={18} />
          </button>
          <button
            onClick={() => emblaApi?.scrollNext()}
            disabled={!canNext}
            aria-label={t("carouselNext")}
            className="flex h-11 w-11 items-center justify-center rounded-full border border-stone-200 bg-white text-stone-600 transition hover:border-grove-400 hover:text-grove-700 disabled:pointer-events-none disabled:opacity-30 dark:border-stone-700 dark:bg-stone-900 dark:text-stone-300 dark:hover:border-grove-500 dark:hover:text-grove-400"
          >
            <ChevronRight size={18} />
          </button>
        </div>
      )}

      {showDots && (
        <div className="mt-6 flex items-center justify-center gap-2">
          {children.map((_, i) => (
            <button
              key={i}
              onClick={() => emblaApi?.scrollTo(i)}
              aria-label={t("slideNumber", { number: i + 1 })}
              className={cn(
                "h-1.5 rounded-full transition-all",
                i === selectedIndex ? "w-6 bg-grove-600" : "w-1.5 bg-stone-300 dark:bg-stone-600",
              )}
            />
          ))}
        </div>
      )}
    </div>
  );
}
