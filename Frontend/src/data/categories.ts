import { Apple, Carrot, Leaf, Milk, Nut, Wheat } from "lucide-react";
import type { Category } from "@/types";

export const categories: Category[] = [
  {
    id: 1,
    name: "Овощи",
    slug: "vegetables",
    description: "Помидоры, огурцы, картофель и другие овощи прямо с грядки",
    icon: Carrot,
    photoKey: "vegetables",
    productCount: 10,
  },
  {
    id: 2,
    name: "Фрукты",
    slug: "fruits",
    description: "Сладкие сезонные фрукты от проверенных садоводов",
    icon: Apple,
    photoKey: "fruits",
    productCount: 10,
  },
  {
    id: 3,
    name: "Зелень",
    slug: "greens",
    description: "Пряные травы и листовая зелень с ярким ароматом",
    icon: Leaf,
    photoKey: "greens",
    productCount: 8,
  },
  {
    id: 4,
    name: "Сухофрукты",
    slug: "dried-fruits",
    description: "Курага, изюм и другие сухофрукты без добавления сахара",
    icon: Wheat,
    photoKey: "dried",
    productCount: 6,
  },
  {
    id: 5,
    name: "Орехи",
    slug: "nuts",
    description: "Грецкий орех, миндаль и фисташки урожая этого года",
    icon: Nut,
    photoKey: "nuts",
    productCount: 5,
  },
  {
    id: 6,
    name: "Молочная продукция",
    slug: "dairy",
    description: "Домашнее молоко, курут и сузьма от фермерских хозяйств",
    icon: Milk,
    photoKey: "dairy",
    productCount: 6,
  },
];

export const getCategoryById = (id: number) => categories.find((c) => c.id === id);
export const getCategoryBySlug = (slug: string) => categories.find((c) => c.slug === slug);
