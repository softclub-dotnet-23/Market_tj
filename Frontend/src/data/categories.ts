// Раньше здесь были захардкожены 6 категорий — теперь тонкая обёртка над
// реальным каталогом (см. data/catalogStore.ts), которая сохраняет прежнее
// имя экспорта, чтобы не переписывать всех потребителей.
export { useCatalogCategories as useCategories } from "@/data/catalogStore";
