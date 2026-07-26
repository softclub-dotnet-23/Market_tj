import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

export function StatusBadge({
  label,
  className,
  icon: Icon,
}: {
  label: string;
  className: string;
  icon?: LucideIcon;
}) {
  return (
    <span className={cn("inline-flex items-center gap-1.5 rounded-full px-3 py-1.5 text-xs font-semibold", className)}>
      {Icon && <Icon size={12} />}
      {label}
    </span>
  );
}
