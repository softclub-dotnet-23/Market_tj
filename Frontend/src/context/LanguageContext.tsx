import { createContext, useCallback, useContext, useRef, useState } from "react";
import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { cn } from "@/lib/utils";
import type { SupportedLanguage } from "@/lib/i18n";

// Смена языка через этот контекст делает плавный кросс-фейд всей страницы:
// контент гаснет → текст меняется (пока невидим) → плавно проявляется обратно.
const SwitchLanguageContext = createContext<(code: SupportedLanguage) => void>(() => {});

export function useSwitchLanguage() {
  return useContext(SwitchLanguageContext);
}

const FADE_OUT_MS = 180;

export function LanguageProvider({ children }: { children: ReactNode }) {
  const { i18n } = useTranslation();
  const [fading, setFading] = useState(false);
  const timers = useRef<number[]>([]);

  const switchLanguage = useCallback(
    (code: SupportedLanguage) => {
      if (code === (i18n.resolvedLanguage ?? i18n.language)) return;

      // Пользователям с prefers-reduced-motion — мгновенная смена без анимации.
      if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
        i18n.changeLanguage(code);
        return;
      }

      timers.current.forEach(clearTimeout);
      timers.current = [];
      setFading(true);
      timers.current.push(
        window.setTimeout(() => {
          i18n.changeLanguage(code);
          timers.current.push(window.setTimeout(() => setFading(false), 40));
        }, FADE_OUT_MS),
      );
    },
    [i18n],
  );

  return (
    <SwitchLanguageContext.Provider value={switchLanguage}>
      <div
        className={cn(
          "transition-opacity duration-200 ease-out",
          fading ? "pointer-events-none opacity-0" : "opacity-100",
        )}
      >
        {children}
      </div>
    </SwitchLanguageContext.Provider>
  );
}
