import { useTranslation } from "react-i18next";
import type { FaqItem } from "@/types";

export interface FaqItemWithKey extends FaqItem {
  groupKey: string;
}

const faqItemsBase: { id: number; groupKey: string }[] = [
  { id: 1, groupKey: "Заказ и доставка" },
  { id: 2, groupKey: "Заказ и доставка" },
  { id: 3, groupKey: "Заказ и доставка" },
  { id: 4, groupKey: "Оплата" },
  { id: 5, groupKey: "Фермерам" },
  { id: 6, groupKey: "Фермерам" },
  { id: 7, groupKey: "Качество" },
  { id: 8, groupKey: "Качество" },
];

export function useFaqItems(): FaqItemWithKey[] {
  const { t } = useTranslation("data");
  return faqItemsBase.map(({ id, groupKey }) => ({
    id,
    groupKey,
    group: t(`faqGroups.${groupKey}`),
    question: t(`faqItems.${id}.question`),
    answer: t(`faqItems.${id}.answer`),
  }));
}

export function useFaqGroupKeys(): string[] {
  return Array.from(new Set(faqItemsBase.map((f) => f.groupKey)));
}
