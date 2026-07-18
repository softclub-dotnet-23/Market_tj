import { useTranslation } from "react-i18next";
import type { Farmer } from "@/types";

type FarmerBase = Omit<Farmer, "region" | "district" | "village" | "bio" | "story" | "tags">;

const farmersBase: FarmerBase[] = [
  { id: 1, farmName: "Гулзор", ownerName: "Файзали Раҳимов", rating: 4.8, reviewCount: 214, productCount: 9, yearsActive: 11, verified: true, responseRate: 98, joinedAt: "2022-03-14" },
  { id: 2, farmName: "Заминдор", ownerName: "Мунира Саидова", rating: 4.9, reviewCount: 356, productCount: 8, yearsActive: 8, verified: true, responseRate: 99, joinedAt: "2021-06-02" },
  { id: 3, farmName: "Себзор", ownerName: "Абдуҷаббор Назаров", rating: 4.9, reviewCount: 189, productCount: 6, yearsActive: 15, verified: true, responseRate: 95, joinedAt: "2020-09-20" },
  { id: 4, farmName: "Ангури Хатлон", ownerName: "Насим Қурбонов", rating: 4.7, reviewCount: 142, productCount: 7, yearsActive: 9, verified: true, responseRate: 92, joinedAt: "2022-01-11" },
  { id: 5, farmName: "Кӯҳдара", ownerName: "Шариф Давлатов", rating: 5.0, reviewCount: 97, productCount: 5, yearsActive: 6, verified: true, responseRate: 90, joinedAt: "2023-04-18" },
  { id: 6, farmName: "Шири Ваҳдат", ownerName: "Гулнора Эргашева", rating: 4.8, reviewCount: 268, productCount: 6, yearsActive: 12, verified: true, responseRate: 97, joinedAt: "2021-11-05" },
  { id: 7, farmName: "Сабзаи Ором", ownerName: "Умеда Холова", rating: 4.9, reviewCount: 176, productCount: 8, yearsActive: 5, verified: true, responseRate: 99, joinedAt: "2023-02-27" },
  { id: 8, farmName: "Мевазор", ownerName: "Далер Ҳакимов", rating: 4.7, reviewCount: 121, productCount: 6, yearsActive: 10, verified: true, responseRate: 93, joinedAt: "2022-08-09" },
  { id: 9, farmName: "Чормағзи Помир", ownerName: "Сафармад Юсупов", rating: 4.9, reviewCount: 88, productCount: 4, yearsActive: 14, verified: true, responseRate: 91, joinedAt: "2020-05-30" },
  { id: 10, farmName: "Гулбоғи Работ", ownerName: "Зулфия Бобоева", rating: 4.8, reviewCount: 133, productCount: 5, yearsActive: 13, verified: true, responseRate: 96, joinedAt: "2021-10-16" },
  { id: 11, farmName: "Гринхаус Душанбе", ownerName: "Сомон Латипов", rating: 4.6, reviewCount: 74, productCount: 4, yearsActive: 3, verified: true, responseRate: 100, joinedAt: "2024-02-08" },
];

export function useFarmers(): Farmer[] {
  const { t } = useTranslation("data");
  return farmersBase.map((f) => ({
    ...f,
    region: t(`farmers.${f.id}.region`),
    district: t(`farmers.${f.id}.district`),
    village: t(`farmers.${f.id}.village`),
    bio: t(`farmers.${f.id}.bio`),
    story: t(`farmers.${f.id}.story`),
    tags: t(`farmers.${f.id}.tags`, { returnObjects: true }) as string[],
  }));
}
