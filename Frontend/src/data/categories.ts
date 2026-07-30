import { useTranslation } from "react-i18next";
import { useCatalogCategories } from "@/data/catalogStore";

// Название категории теперь хранится на бэкенде сразу в 3 языках
// (Category.Name/NameTj/NameEn, см. миграцию AddCategoryTranslations —
// админ обязан заполнить все три при создании категории в /admin/catalog),
// поэтому просто выбираем нужное поле по текущему языку интерфейса.
// Описание же по-прежнему хранится одной строкой (Category.Description) —
// у изначальных 6 категорий, засеянных ещё до подключения бэкенда, есть
// готовые переводы в locales/{ru,tj,en}/data.json, переиспользуем их по
// известному сопоставлению; у новых категорий, добавленных админом,
// описание останется на том языке, на котором его ввели (это не отличается
// от названий товаров/хозяйств — их описания тоже никто, кроме автора, не
// вправе "переводить").
const DESCRIPTION_I18N_KEY_BY_NAME: Record<string, string> = {
  "Овощи": "vegetables",
  "Фрукты": "fruits",
  "Зелень": "greens",
  "Сухофрукты": "dried-fruits",
  "Орехи": "nuts",
  "Молочная продукция": "dairy",
};

export function useCategories() {
  const { t, i18n } = useTranslation("data");
  const categories = useCatalogCategories();
  return categories.map((c) => {
    const localizedName = i18n.language === "tj" ? (c.nameTj ?? c.name) : i18n.language === "en" ? (c.nameEn ?? c.name) : c.name;
    const descriptionKey = DESCRIPTION_I18N_KEY_BY_NAME[c.name];
    return {
      ...c,
      name: localizedName,
      description: descriptionKey ? t(`categories.${descriptionKey}.description`, { defaultValue: c.description }) : c.description,
    };
  });
}
