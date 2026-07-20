import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import LanguageDetector from "i18next-browser-languagedetector";

export const SUPPORTED_LANGUAGES = ["tj", "ru"] as const;
export type SupportedLanguage = (typeof SUPPORTED_LANGUAGES)[number];

const LANGUAGE_STORAGE_KEY = "market-tj-language";

// Каждый locales/<lng>/<ns>.json подключается автоматически — при добавлении
// нового namespace-файла не нужно править этот файл.
const localeModules = import.meta.glob<{ default: Record<string, unknown> }>("../locales/*/*.json", {
  eager: true,
});

const resources: Record<string, Record<string, Record<string, unknown>>> = {};

for (const path in localeModules) {
  const match = path.match(/\.\.\/locales\/([a-z]+)\/([a-zA-Z0-9-]+)\.json$/);
  if (!match) continue;
  const [, lng, ns] = match;
  resources[lng] ??= {};
  resources[lng][ns] = localeModules[path].default;
}

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    fallbackLng: "tj",
    supportedLngs: SUPPORTED_LANGUAGES,
    ns: Object.keys(resources.tj ?? {}),
    defaultNS: "common",
    interpolation: { escapeValue: false },
    detection: {
      // Только явный выбор пользователя переопределяет язык по умолчанию (tj) —
      // язык браузера намеренно не учитываем (раздел ТЗ: таджикский основной).
      order: ["localStorage"],
      caches: ["localStorage"],
      lookupLocalStorage: LANGUAGE_STORAGE_KEY,
    },
  });

export default i18n;
