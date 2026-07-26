// Раньше здесь был захардкоженный список из ~50 товаров — теперь тонкая
// обёртка над реальным каталогом (см. data/catalogStore.ts), которая
// сохраняет прежние имена экспортов, чтобы не переписывать всех потребителей.
export { useCatalogProducts as useProducts, getCatalogProductById as getProductById, useCatalogLoaded } from "@/data/catalogStore";
