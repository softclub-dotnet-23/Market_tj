import { Sprout } from "lucide-react";
import { cn } from "@/lib/utils";

export function PhotoTile({
  src,
  alt,
  className,
  imgClassName,
}: {
  src?: string;
  alt: string;
  className?: string;
  imgClassName?: string;
}) {
  return (
    <div className={cn("relative flex items-center justify-center overflow-hidden bg-stone-100 dark:bg-stone-800", className)}>
      {src ? (
        <img
          src={src}
          alt={alt}
          loading="lazy"
          draggable={false}
          className={cn("h-full w-full object-cover", imgClassName)}
        />
      ) : (
        <Sprout className="h-1/3 w-1/3 text-stone-300 dark:text-stone-600" />
      )}
    </div>
  );
}
