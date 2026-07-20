import type { Review } from "@/types";
import i18n from "@/lib/i18n";
import { daysAgoISO } from "@/lib/utils";
import { getProductById } from "@/data/products";

const CLEAN_NAMES = [
  "Азиз Каримов", "Мадина Юсупова", "Далер Раҳимов", "Нигина Собирова",
  "Фаррух Толибов", "Зарина Ҳакимова", "Умар Шарипов", "Севара Назарова",
  "Искандар Одинаев", "Малика Турсунова", "Бахтиёр Раджабов", "Диловар Эргашев",
  "Гулру Файзиева", "Раҳматулло Сафаров", "Ситора Абдуллоева", "Комрон Латипов",
];

function seededRandom(seed: number) {
  let value = seed % 2147483647;
  if (value <= 0) value += 2147483646;
  return () => {
    value = (value * 16807) % 2147483647;
    return (value - 1) / 2147483646;
  };
}

function pick<T>(arr: T[], rand: () => number) {
  return arr[Math.floor(rand() * arr.length)];
}

function buildReviewsForProduct(productId: number): Review[] {
  const product = getProductById(productId);
  if (!product) return [];
  const rand = seededRandom(productId * 7919);
  const count = Math.min(6, Math.max(3, Math.round(product.reviewCount / 20)));
  const usedNames = new Set<string>();

  const commentsHigh = i18n.t("reviews.commentsHigh", { ns: "data", returnObjects: true }) as string[];
  const commentsMid = i18n.t("reviews.commentsMid", { ns: "data", returnObjects: true }) as string[];
  const replies = i18n.t("reviews.replies", { ns: "data", returnObjects: true }) as string[];

  return Array.from({ length: count }).map((_, i) => {
    let name = pick(CLEAN_NAMES, rand);
    while (usedNames.has(name) && usedNames.size < CLEAN_NAMES.length) {
      name = pick(CLEAN_NAMES, rand);
    }
    usedNames.add(name);

    const isHigh = rand() > 0.28;
    const rating = isHigh ? (rand() > 0.5 ? 5 : 4) : 3;
    const comment = isHigh ? pick(commentsHigh, rand) : pick(commentsMid, rand);
    const hasReply = i === 0 && rand() > 0.4;

    return {
      id: productId * 100 + i,
      productId,
      customerName: name,
      rating,
      comment,
      createdAt: daysAgoISO(2 + Math.floor(rand() * 60)),
      verifiedPurchase: rand() > 0.15,
      helpfulCount: Math.floor(rand() * 24),
      farmerReply: hasReply
        ? { message: pick(replies, rand), createdAt: daysAgoISO(1 + Math.floor(rand() * 30)) }
        : undefined,
    };
  });
}

// Функция детерминирована по productId (seeded random), поэтому кэш не нужен —
// пересчёт при каждом вызове гарантирует актуальный язык комментариев.
export function getReviewsForProduct(productId: number): Review[] {
  return buildReviewsForProduct(productId);
}
