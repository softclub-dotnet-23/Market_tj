import { createContext, useContext, useState } from "react";
import type { ReactNode } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";

const AccordionContext = createContext<{
  openItems: Set<string>;
  toggle: (id: string) => void;
} | null>(null);

export function Accordion({
  children,
  className,
  allowMultiple = false,
  defaultOpen,
}: {
  children: ReactNode;
  className?: string;
  allowMultiple?: boolean;
  defaultOpen?: string;
}) {
  const [openItems, setOpenItems] = useState<Set<string>>(
    new Set(defaultOpen ? [defaultOpen] : []),
  );

  const toggle = (id: string) => {
    setOpenItems((prev) => {
      const next = new Set(allowMultiple ? prev : []);
      if (prev.has(id)) {
        if (allowMultiple) next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  return (
    <AccordionContext.Provider value={{ openItems, toggle }}>
      <div className={cn("flex flex-col divide-y divide-stone-100 dark:divide-stone-800", className)}>
        {children}
      </div>
    </AccordionContext.Provider>
  );
}

export function AccordionItem({
  id,
  question,
  children,
}: {
  id: string;
  question: ReactNode;
  children: ReactNode;
}) {
  const ctx = useContext(AccordionContext);
  if (!ctx) throw new Error("AccordionItem must be used within Accordion");
  const isOpen = ctx.openItems.has(id);

  return (
    <div className="py-1.5">
      <button
        onClick={() => ctx.toggle(id)}
        className="flex w-full items-center justify-between gap-4 py-4 text-left"
        aria-expanded={isOpen}
      >
        <span className="text-[15px] font-medium text-stone-900 sm:text-base dark:text-stone-100">{question}</span>
        <span
          className={cn(
            "flex h-8 w-8 shrink-0 items-center justify-center rounded-full border border-stone-200 text-stone-500 transition-all duration-300 dark:border-stone-700 dark:text-stone-400",
            isOpen && "rotate-180 border-grove-300 bg-grove-50 text-grove-700 dark:border-grove-700 dark:bg-grove-950 dark:text-grove-300",
          )}
        >
          <ChevronDown size={16} />
        </span>
      </button>
      <AnimatePresence initial={false}>
        {isOpen && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: "auto", opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.35, ease: [0.25, 1, 0.5, 1] }}
            className="overflow-hidden"
          >
            <div className="pb-4 pr-10 text-[15px] leading-relaxed text-stone-500 dark:text-stone-400">{children}</div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
