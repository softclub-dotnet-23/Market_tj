import { useTranslation } from "react-i18next";
import { cn } from "@/lib/utils";
import { SUPPORTED_LANGUAGES, type SupportedLanguage } from "@/lib/i18n";

const LANGUAGE_LABELS: Record<SupportedLanguage, string> = {
  tj: "ТҶ",
  ru: "РУ",
};

export function LanguageSwitcher({ className }: { className?: string }) {
  const { t, i18n } = useTranslation();
  const current = (i18n.resolvedLanguage ?? i18n.language) as SupportedLanguage;

  const switchLanguage = () => {
    const currentIndex = SUPPORTED_LANGUAGES.indexOf(current);
    const next = SUPPORTED_LANGUAGES[(currentIndex + 1) % SUPPORTED_LANGUAGES.length];
    i18n.changeLanguage(next);
  };

  return (
    <button
      onClick={switchLanguage}
      aria-label={t("actions.changeLanguage")}
      title={t("actions.changeLanguage")}
      className={cn(
        "flex h-10 w-10 items-center justify-center rounded-full text-xs font-semibold text-stone-600 transition hover:bg-stone-100 dark:text-stone-300 dark:hover:bg-stone-800",
        className,
      )}
    >
      {LANGUAGE_LABELS[current]}
    </button>
  );
}
