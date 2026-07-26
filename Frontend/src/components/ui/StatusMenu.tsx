import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { AnimatePresence, motion } from "framer-motion";
import { Check, ChevronDown, Lock } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

export interface StatusMenuOption {
  value: number;
  label: string;
  className: string;
  icon?: LucideIcon;
}

// Таблицы заказов лежат в контейнере с overflow-x-auto (горизонтальный скролл
// на узких экранах) — из-за этого обычное position:absolute внутри строки
// обрезалось по вертикали (CSS так устроен: overflow-x не visible всегда
// тянет за собой overflow-y). Рисуем меню через портал в document.body с
// position:fixed по реальным координатам кнопки — тот же приём, что уже
// использует Modal.tsx, только без фонового оверлея.
export function StatusMenu({
  value,
  options,
  onChange,
  busy,
  lockedLabel,
}: {
  value: number;
  options: StatusMenuOption[];
  onChange: (value: number) => void;
  busy?: boolean;
  lockedLabel?: string;
}) {
  const [open, setOpen] = useState(false);
  const [coords, setCoords] = useState({ top: 0, left: 0 });
  const buttonRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLUListElement>(null);
  const current = options.find((o) => o.value === value);
  const isLocked = options.length <= 1;

  useEffect(() => {
    function onClick(e: MouseEvent) {
      const target = e.target as Node;
      if (buttonRef.current?.contains(target) || menuRef.current?.contains(target)) return;
      setOpen(false);
    }
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, []);

  const toggleOpen = () => {
    if (!open && buttonRef.current) {
      const rect = buttonRef.current.getBoundingClientRect();
      setCoords({ top: rect.bottom + 6, left: rect.left });
    }
    setOpen((o) => !o);
  };

  return (
    <>
      <button
        ref={buttonRef}
        type="button"
        disabled={isLocked || busy}
        onClick={toggleOpen}
        title={isLocked ? lockedLabel : undefined}
        className={cn(
          "inline-flex items-center gap-1.5 rounded-full px-3 py-1.5 text-xs font-semibold transition disabled:cursor-not-allowed",
          current?.className ?? options[0]?.className,
          busy && "opacity-60",
          !isLocked && !busy && "cursor-pointer hover:brightness-95",
        )}
      >
        {current?.icon && <current.icon size={12} />}
        {current?.label}
        {isLocked ? <Lock size={11} className="opacity-60" /> : <ChevronDown size={13} className={cn("transition-transform", open && "rotate-180")} />}
      </button>
      {createPortal(
        <AnimatePresence>
          {open && !isLocked && (
            <motion.ul
              ref={menuRef}
              initial={{ opacity: 0, y: -6, scale: 0.97 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: -6, scale: 0.97 }}
              transition={{ duration: 0.15 }}
              style={{ position: "fixed", top: coords.top, left: coords.left }}
              className="z-100 min-w-44 overflow-hidden rounded-xl border border-stone-100 bg-white p-1.5 shadow-(--shadow-lifted) dark:border-stone-800 dark:bg-stone-900"
            >
              {options.map((opt) => (
                <li key={opt.value}>
                  <button
                    type="button"
                    onClick={() => {
                      if (opt.value !== value) onChange(opt.value);
                      setOpen(false);
                    }}
                    className="flex w-full items-center justify-between gap-2 rounded-lg px-2 py-1.5 text-left transition hover:bg-stone-50 dark:hover:bg-stone-800"
                  >
                    <span className={cn("inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold", opt.className)}>
                      {opt.icon && <opt.icon size={12} />}
                      {opt.label}
                    </span>
                    {opt.value === value && <Check size={14} className="shrink-0 text-grove-600 dark:text-grove-400" />}
                  </button>
                </li>
              ))}
            </motion.ul>
          )}
        </AnimatePresence>,
        document.body,
      )}
    </>
  );
}
