import type { ReactNode } from "react";
import { createPortal } from "react-dom";
import { AnimatePresence, motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { X } from "lucide-react";

// Тот же приём портала+бэкдропа+слайда, что уже есть у публичного
// MobileMenu.tsx — просто выезжает слева (сайдбар панелей и так слева), а
// содержимое (брендинг+нав+логаут) передаётся снаружи, чтобы не дублировать
// бейдж-логику каждого Layout — она у каждой панели своя.
export function PanelMobileDrawer({ open, onClose, children }: { open: boolean; onClose: () => void; children: ReactNode }) {
  const { t } = useTranslation("common");

  return createPortal(
    <AnimatePresence>
      {open && (
        <div className="fixed inset-0 z-100 lg:hidden">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            className="absolute inset-0 bg-stone-950/50 backdrop-blur-sm"
          />
          <motion.div
            initial={{ x: "-100%" }}
            animate={{ x: 0 }}
            exit={{ x: "-100%" }}
            transition={{ duration: 0.3, ease: [0.25, 1, 0.5, 1] }}
            className="absolute left-0 top-0 flex h-full w-72 max-w-[85vw] flex-col bg-white dark:bg-stone-900"
          >
            <button
              onClick={onClose}
              aria-label={t("actions.close")}
              className="absolute right-3 top-3 z-10 flex h-8 w-8 items-center justify-center rounded-full bg-stone-100 text-stone-500 dark:bg-stone-800 dark:text-stone-400"
            >
              <X size={16} />
            </button>
            {children}
          </motion.div>
        </div>
      )}
    </AnimatePresence>,
    document.body,
  );
}
