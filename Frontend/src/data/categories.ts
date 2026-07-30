import { useTranslation } from "react-i18next";
import { useCatalogCategories } from "@/data/catalogStore";

// Реальные категории с бэкенда (Category.Name) хранятся одной строкой без
// локализации — но это ровно тот же фиксированный список из 6 категорий,
// что и в locales/{ru,tj}/data.json (там остались готовые переводы ещё с
// mock-фазы фронтенда, до подключения бэкенда, — просто их перестали
// использовать после перехода на catalogStore.ts). Раз названия совпадают
// один в один, честно переиспользуем уже готовые переводы по известному
// сопоставлению, а не переводим заново и не изобретаем локализацию для
// произвольного текста (в отличие от названий товаров/хозяйств, которые
// вводят сами фермеры и которые никто, кроме них, не вправе "переводить").
const I18N_KEY_BY_NAME: Record<string, string> = {
  "Овощи": "vegetables",
  "Фрукты": "fruits",
  "Зелень": "greens",
  "Сухофрукты": "dried-fruits",
  "Орехи": "nuts",
  "Молочная продукция": "dairy",
};

export function useCategories() {
  const { t } = useTranslation("data");
  const categories = useCatalogCategories();
  return categories.map((c) => {
    const key = I18N_KEY_BY_NAME[c.name];
    if (!key) return c;
    return {
      ...c,
      name: t(`categories.${key}.name`, { defaultValue: c.name }),
      description: t(`categories.${key}.description`, { defaultValue: c.description }),
    };
  });
}
