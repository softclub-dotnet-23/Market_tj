import { Apple, Carrot, Leaf, Milk, Nut, Wheat } from "lucide-react";
import { useTranslation } from "react-i18next";
import type { Category } from "@/types";

type CategoryBase = Omit<Category, "name" | "description">;

const categoriesBase: CategoryBase[] = [
  { id: 1, slug: "vegetables", icon: Carrot, photoKey: "vegetables", productCount: 10 },
  { id: 2, slug: "fruits", icon: Apple, photoKey: "fruits", productCount: 10 },
  { id: 3, slug: "greens", icon: Leaf, photoKey: "greens", productCount: 8 },
  { id: 4, slug: "dried-fruits", icon: Wheat, photoKey: "dried", productCount: 6 },
  { id: 5, slug: "nuts", icon: Nut, photoKey: "nuts", productCount: 5 },
  { id: 6, slug: "dairy", icon: Milk, photoKey: "dairy", productCount: 6 },
];

export function useCategories(): Category[] {
  const { t } = useTranslation("data");
  return categoriesBase.map((c) => ({
    ...c,
    name: t(`categories.${c.slug}.name`),
    description: t(`categories.${c.slug}.description`),
  }));
}
