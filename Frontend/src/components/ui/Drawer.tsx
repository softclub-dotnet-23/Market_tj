import { useEffect } from "react";
import type { ReactNode } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { X } from "lucide-react";
import { createPortal } from "react-dom";
import { cn } from "@/lib/utils";

// Правая выезжающая панель — тот же портал/фон/тени, что у Modal.tsx, только
// анимация "слайд справа" вместо "scale из центра". Добавлена по прямому
// запросу пользователя (2026-08-02, назначение курьера) — раньше в проекте
// не было отдельного drawer-примитива, только центрированный Modal.
export function Drawer({
  open,
  onClose,
  children,
  className,
  title,
}: {
  open: boolean;
  onClose: () => void;
  children: ReactNode;
  className?: string;
  title?: ReactNode;
}) {
  const { t } = useTranslation("common");
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && onClose();
    document.addEventListener("keydown", onKey);
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", onKey);
      document.body.style.overflow = "";
    };
  }, [open, onClose]);

  return createPortal(
    <AnimatePresence>
      {open && (
        <div className="fixed inset-0 z-100 flex justify-end">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.25 }}
            onClick={onClose}
            className="absolute inset-0 bg-stone-950/60 backdrop-blur-sm"
          />
          <motion.div
            initial={{ x: "100%" }}
            animate={{ x: 0 }}
            exit={{ x: "100%" }}
            transition={{ type: "spring", stiffness: 320, damping: 32, mass: 0.9 }}
            className={cn(
              "relative flex h-full w-full max-w-lg flex-col overflow-hidden bg-white shadow-(--shadow-lifted) sm:max-w-xl dark:bg-stone-900",
              className,
            )}
          >
            <div className="flex shrink-0 items-center justify-between gap-3 border-b border-stone-100 px-6 py-5 dark:border-stone-800">
              <div className="min-w-0 flex-1 font-display text-lg text-stone-900 dark:text-stone-50">{title}</div>
              <button
                onClick={onClose}
                aria-label={t("actions.close")}
                className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-stone-100 text-stone-500 transition hover:bg-stone-200 hover:text-stone-800 dark:bg-stone-800 dark:text-stone-400 dark:hover:bg-stone-700 dark:hover:text-stone-100"
              >
                <X size={16} />
              </button>
            </div>
            <div className="flex-1 overflow-y-auto">{children}</div>
          </motion.div>
        </div>
      )}
    </AnimatePresence>,
    document.body,
  );
}
