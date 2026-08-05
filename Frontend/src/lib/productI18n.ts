// Раздел "товар на 3 языках" (2026-08-05) — title/description на бэкенде
// хранятся отдельными полями (Ru — основной/обязательный, Tj/En —
// опциональные, автоматически переводятся Groq'ом на бэкенде, если фермер
// не заполнил их сам). Разрешаем язык отображения ЗДЕСЬ, в компонентах,
// через текущий i18n.language — а не один раз при загрузке каталога — иначе
// переключение языка UI не обновило бы уже загруженные названия товаров
// (catalogStore.ts кэширует каталог на уровне модуля, а не по языку).
// Duck-typed (не строго Product) — те же поля приходят и на других DTO,
// например AdminProductListingDto в data/adminEntities.ts.
export function getLocalizedTitle(product: { title: string; titleTj?: string; titleEn?: string }, lang: string): string {
  if (lang === "tj") return product.titleTj || product.title;
  if (lang === "en") return product.titleEn || product.title;
  return product.title;
}

export function getLocalizedDescription(product: { description: string; descriptionTj?: string; descriptionEn?: string }, lang: string): string {
  if (lang === "tj") return product.descriptionTj || product.description;
  if (lang === "en") return product.descriptionEn || product.description;
  return product.description;
}

// Та же логика обрезки (140 символов), что и у product.shortDescription в
// catalogStore.ts — здесь для случаев, когда нужна короткая версия
// ЛОКАЛИЗОВАННОГО описания, а не всегда русского.
export function getLocalizedShortDescription(product: { description: string; descriptionTj?: string; descriptionEn?: string }, lang: string): string {
  const description = getLocalizedDescription(product, lang);
  return description.length > 140 ? `${description.slice(0, 140)}…` : description;
}
