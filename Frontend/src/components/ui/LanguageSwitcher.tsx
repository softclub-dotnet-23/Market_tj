import { useEffect, useRef, useState } from "react";
import type { FC } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { Check, ChevronDown, Globe } from "lucide-react";
import { cn } from "@/lib/utils";
import type { SupportedLanguage } from "@/lib/i18n";

// Флаги — inline SVG, а не emoji: на Windows флаг-эмодзи не рендерятся (показываются буквами).
function FlagTJ() {
  return (
    <svg viewBox="0 0 24 24" className="h-full w-full">
      <rect width="24" height="7" fill="#CC0000" />
      <rect y="7" width="24" height="10" fill="#FFFFFF" />
      <rect y="17" width="24" height="7" fill="#006600" />
      <g fill="#F8C300">
        <circle cx="10" cy="9.6" r="0.55" />
        <circle cx="12" cy="9.3" r="0.55" />
        <circle cx="14" cy="9.6" r="0.55" />
        <path d="M8.6 13.9v-2.6l1.75 1.3 1.65-2.2 1.65 2.2 1.75-1.3v2.6z" />
      </g>
    </svg>
  );
}

function FlagRU() {
  return (
    <svg viewBox="0 0 24 24" className="h-full w-full">
      <rect width="24" height="8" fill="#FFFFFF" />
      <rect y="8" width="24" height="8" fill="#0039A6" />
      <rect y="16" width="24" height="8" fill="#D52B1E" />
    </svg>
  );
}

interface LanguageOption {
  code: SupportedLanguage;
  name: string;
  short: string;
  Flag: FC;
}

// Названия языков всегда на самом языке — не переводятся. Таджикский первый (основной).
const LANGUAGES: LanguageOption[] = [
  { code: "tj", name: "Тоҷикӣ", short: "ТҶ", Flag: FlagTJ },
  { code: "ru", name: "Русский", short: "РУ", Flag: FlagRU },
];

export function LanguageSwitcher({ className }: { className?: string }) {
  const { t, i18n } = useTranslation();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const current = (i18n.resolvedLanguage ?? i18n.language) as SupportedLanguage;
  const active = LANGUAGES.find((l) => l.code === current) ?? LANGUAGES[0];

  useEffect(() => {
    function onClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") setOpen(false);
    }
    document.addEventListener("mousedown", onClick);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onClick);
      document.removeEventListener("keydown", onKey);
    };
  }, []);

  useEffect(() => {
    document.documentElement.lang = current;
  }, [current]);

  const selectLanguage = (code: SupportedLanguage) => {
    i18n.changeLanguage(code);
    setOpen(false);
  };

  return (
    <div ref={ref} className={cn("relative", className)}>
      <button
        onClick={() => setOpen((o) => !o)}
        aria-label={t("actions.changeLanguage")}
        aria-haspopup="listbox"
        aria-expanded={open}
        className={cn(
          "flex h-10 items-center gap-1.5 rounded-full pl-2 pr-2.5 text-stone-600 transition hover:bg-stone-100 dark:text-stone-300 dark:hover:bg-stone-800",
          open && "bg-stone-100 dark:bg-stone-800",
        )}
      >
        <Globe size={18} className="text-grove-600 dark:text-grove-400" />
        <span className="text-xs font-semibold tabular-nums">{active.short}</span>
        <ChevronDown
          size={14}
          className={cn("text-stone-400 transition-transform dark:text-stone-500", open && "rotate-180")}
        />
      </button>

      <AnimatePresence>
        {open && (
          <motion.ul
            role="listbox"
            initial={{ opacity: 0, y: -6, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -6, scale: 0.98 }}
            transition={{ duration: 0.16, ease: [0.25, 1, 0.5, 1] }}
            className="absolute right-0 top-full z-50 mt-2 w-44 overflow-hidden rounded-2xl border border-stone-100 bg-white p-1.5 shadow-(--shadow-lifted) dark:border-stone-800 dark:bg-stone-900"
          >
            {LANGUAGES.map((lang) => {
              const isActive = lang.code === current;
              return (
                <li key={lang.code} role="option" aria-selected={isActive}>
                  <button
                    onClick={() => selectLanguage(lang.code)}
                    className={cn(
                      "flex w-full items-center gap-3 rounded-xl px-2.5 py-2 text-left text-sm transition",
                      isActive
                        ? "bg-grove-50 font-semibold text-grove-800 dark:bg-grove-950 dark:text-grove-200"
                        : "text-stone-600 hover:bg-stone-50 dark:text-stone-300 dark:hover:bg-stone-800",
                    )}
                  >
                    <span
                      className={cn(
                        "flex h-6 w-6 shrink-0 items-center justify-center overflow-hidden rounded-full ring-1",
                        isActive ? "ring-grove-300 dark:ring-grove-700" : "ring-stone-200 dark:ring-stone-600",
                      )}
                    >
                      <lang.Flag />
                    </span>
                    <span className="flex-1">{lang.name}</span>
                    {isActive && <Check size={15} className="shrink-0 text-grove-600 dark:text-grove-400" />}
                  </button>
                </li>
              );
            })}
          </motion.ul>
        )}
      </AnimatePresence>
    </div>
  );
}
