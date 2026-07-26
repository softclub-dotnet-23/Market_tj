// Раньше здесь был захардкоженный список из 11 выдуманных фермеров — теперь
// тонкая обёртка над реальным каталогом (см. data/catalogStore.ts), которая
// сохраняет прежнее имя экспорта, чтобы не переписывать всех потребителей.
export { useCatalogFarmers as useFarmers, getCatalogFarmerById as getFarmerById } from "@/data/catalogStore";
