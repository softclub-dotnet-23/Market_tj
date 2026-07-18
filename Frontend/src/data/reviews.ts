import type { Review } from "@/types";
import { daysAgoISO } from "@/lib/utils";
import { getProductById } from "@/data/products";

const CLEAN_NAMES = [
  "Азиз Каримов", "Мадина Юсупова", "Далер Раҳимов", "Нигина Собирова",
  "Фаррух Толибов", "Зарина Ҳакимова", "Умар Шарипов", "Севара Назарова",
  "Искандар Одинаев", "Малика Турсунова", "Бахтиёр Раджабов", "Диловар Эргашев",
  "Гулру Файзиева", "Раҳматулло Сафаров", "Ситора Абдуллоева", "Комрон Латипов",
];

const COMMENTS_HIGH = [
  "Заказывали не в первый раз — качество стабильно на высоте, всё свежее и приезжает вовремя.",
  "Очень довольны! Продукт именно такой, каким описан на фото и в карточке. Будем заказывать ещё.",
  "Приятно удивило, насколько всё свежее — чувствуется, что собирали недавно, а не лежало на складе неделю.",
  "Отличный вкус и честный вес — ничего не подгнило, упаковано аккуратно.",
  "Курьер приехал в срок, продукт в идеальном состоянии. Однозначно рекомендую этого фермера.",
  "Одни из лучших продуктов, что пробовали за последнее время. Видно, что фермер вкладывается в качество.",
];

const COMMENTS_MID = [
  "В целом хорошо, но пара штук были немного мельче, чем хотелось бы. В остальном претензий нет.",
  "Свежее, вкусное, но упаковка могла быть и понадёжнее — один пакет немного порвался при доставке.",
  "Качество хорошее, соответствует цене. Возьмём ещё раз, но, возможно, у другого объёма партии.",
];

const REPLIES = [
  "Спасибо за отзыв! Рады, что вам понравилось — будем поддерживать качество и дальше.",
  "Благодарим за обратную связь, учтём пожелания по упаковке в следующей поставке.",
  "Спасибо, что выбираете наше хозяйство! Ждём вас снова.",
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

  return Array.from({ length: count }).map((_, i) => {
    let name = pick(CLEAN_NAMES, rand);
    while (usedNames.has(name) && usedNames.size < CLEAN_NAMES.length) {
      name = pick(CLEAN_NAMES, rand);
    }
    usedNames.add(name);

    const isHigh = rand() > 0.28;
    const rating = isHigh ? (rand() > 0.5 ? 5 : 4) : 3;
    const comment = isHigh ? pick(COMMENTS_HIGH, rand) : pick(COMMENTS_MID, rand);
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
        ? { message: pick(REPLIES, rand), createdAt: daysAgoISO(1 + Math.floor(rand() * 30)) }
        : undefined,
    };
  });
}

const cache = new Map<number, Review[]>();

export function getReviewsForProduct(productId: number): Review[] {
  if (!cache.has(productId)) {
    cache.set(productId, buildReviewsForProduct(productId));
  }
  return cache.get(productId)!;
}
