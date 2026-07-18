import type { ReactNode } from "react";
import { motion } from "framer-motion";
import { cn } from "@/lib/utils";

export function EmptyState({
  icon,
  title,
  description,
  action,
  className,
}: {
  icon: ReactNode;
  title: string;
  description?: string;
  action?: ReactNode;
  className?: string;
}) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.4 }}
      className={cn(
        "flex flex-col items-center justify-center gap-4 rounded-3xl border border-dashed border-stone-300 bg-stone-50/60 px-6 py-16 text-center dark:border-stone-700 dark:bg-stone-900/60",
        className,
      )}
    >
      <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-white text-grove-600 shadow-(--shadow-soft) dark:bg-stone-800 dark:text-grove-400">
        {icon}
      </div>
      <div className="flex flex-col gap-1.5">
        <h3 className="font-display text-xl text-stone-900 dark:text-stone-50">{title}</h3>
        {description && <p className="max-w-sm text-sm text-stone-500 dark:text-stone-400">{description}</p>}
      </div>
      {action}
    </motion.div>
  );
}
