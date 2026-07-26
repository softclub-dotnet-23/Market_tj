import type { Review } from "@/types";
import { getCatalogProductById, getCatalogReviewsByFarmerId } from "@/data/catalogStore";

// Раньше здесь были процедурно сгенерированные фейковые отзывы (случайные
// имена, комментарии из фиксированного набора фраз) — теперь настоящие Review
// с бэкенда. В базе отзыв привязан к фермеру целиком, не к конкретному
// объявлению (раздел 8.13 ТЗ — Review.FarmerId, без ProductListingId), так
// что "отзывы о товаре" — это на самом деле отзывы о фермере, который его
// продаёт; сигнатура функции не изменилась, чтобы не переписывать вызовы.
export function getReviewsForProduct(productId: number): Review[] {
  const product = getCatalogProductById(productId);
  if (!product) return [];
  return getCatalogReviewsByFarmerId(product.farmerId);
}
