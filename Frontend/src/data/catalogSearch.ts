import { apiGet, resolveMediaUrl } from "@/lib/api";
import { slugify } from "@/lib/utils";
import { productPhotos } from "@/assets/photos";
import { ListingStatus } from "@/data/farmer";
import { FALLBACK_PHOTO_ID_BY_PRODUCT_NAME } from "@/data/productPhotoFallback";
import type { Product } from "@/types";

// Публичный каталог (раздел 13.5 ТЗ): фильтрация/сортировка/пагинация теперь
// выполняются на бэкенде (GET /product-listings/search), а не в браузере над
// заранее выгруженным полным списком (так до сих пор работают все ОСТАЛЬНЫЕ
// потребители — Home/ProductDetails/FarmerCard/MegaMenu/About — через
// catalogStore.ts, их сознательно не трогаем). Этот модуль обслуживает
// ТОЛЬКО страницу Catalog.tsx.

export interface CatalogSearchFilter {
  categoryIds?: number[];
  region?: string;
  farmerId?: number | null;
  priceMin?: number;
  priceMax?: number;
  search?: string;
  sortBy?: string;
  listingIds?: number[];
  pageNumber: number;
  pageSize: number;
}

export interface CatalogSearchResult {
  items: Product[];
  totalCount: number;
}

interface RawCatalogListing {
  id: number;
  farmerProfileId: number;
  categoryId: number;
  unit: string;
  title: string;
  titleTj: string | null;
  titleEn: string | null;
  description: string | null;
  descriptionTj: string | null;
  descriptionEn: string | null;
  retailPricePerKg: number;
  wholesalePricePerKg: number | null;
  wholesaleMinimumQuantity: number | null;
  availableQuantity: number;
  minimumOrderQuantity: number;
  harvestDate: string | null;
  expectedHarvestDate: string | null;
  freshnessDaysAgo: number | null;
  qualityGrade: string;
  region: string;
  district: string;
  status: number;
  createdAt: string;
  rating: number;
  orderCount: number;
}

interface RawImage {
  productListingId: number;
  imageUrl: string;
  isMain: boolean;
}

interface RawPagedResult<T> {
  items: T[];
  totalCount: number;
}

// /product-images — тот же публичный эндпоинт, что грузит catalogStore.ts,
// но здесь свой отдельный маленький кэш: страница поиска не должна зависеть
// от того, успел ли где-то ещё прогрузиться весь клиентский каталог.
let imagesPromise: Promise<Map<number, RawImage[]>> | null = null;
function loadImagesByListingId(): Promise<Map<number, RawImage[]>> {
  if (!imagesPromise) {
    imagesPromise = apiGet<RawImage[]>("/product-images")
      .then((images) => {
        const map = new Map<number, RawImage[]>();
        for (const img of images) {
          const list = map.get(img.productListingId) ?? [];
          list.push(img);
          map.set(img.productListingId, list);
        }
        return map;
      })
      .catch((err) => {
        imagesPromise = null;
        throw err;
      });
  }
  return imagesPromise;
}

function buildQuery(filter: CatalogSearchFilter): string {
  const params = new URLSearchParams();
  params.set("pageNumber", String(filter.pageNumber));
  params.set("pageSize", String(filter.pageSize));
  if (filter.categoryIds?.length) params.set("categoryIds", filter.categoryIds.join(","));
  if (filter.region) params.set("region", filter.region);
  if (filter.farmerId) params.set("farmerId", String(filter.farmerId));
  if (filter.priceMin !== undefined) params.set("priceMin", String(filter.priceMin));
  if (filter.priceMax !== undefined) params.set("priceMax", String(filter.priceMax));
  if (filter.search) params.set("search", filter.search);
  if (filter.sortBy) params.set("sortBy", filter.sortBy);
  if (filter.listingIds?.length) params.set("listingIds", filter.listingIds.join(","));
  return params.toString();
}

export async function searchCatalog(filter: CatalogSearchFilter): Promise<CatalogSearchResult> {
  const [page, imagesByListingId] = await Promise.all([
    apiGet<RawPagedResult<RawCatalogListing>>(`/product-listings/search?${buildQuery(filter)}`),
    loadImagesByListingId(),
  ]);

  // "Хит продаж" — топ 20% ЭТОЙ страницы результатов, а не всего каталога:
  // сортировка и пагинация теперь на бэкенде, единый глобальный порог (как в
  // catalogStore.ts, который считает его разом по ~200 товарам) для одной
  // отфильтрованной страницы результатов означал бы другое число и вводил
  // бы в заблуждение.
  const orderCounts = page.items.map((l) => l.orderCount).sort((a, b) => b - a);
  const bestsellerRank = Math.max(0, Math.ceil(orderCounts.length * 0.2) - 1);
  const bestsellerThreshold = orderCounts[bestsellerRank] ?? 0;

  const items: Product[] = page.items.map((listing) => {
    const images = [...(imagesByListingId.get(listing.id) ?? [])].sort((a, b) => Number(b.isMain) - Number(a.isMain));
    const matchedFallbackName = Object.keys(FALLBACK_PHOTO_ID_BY_PRODUCT_NAME).find((name) => listing.title.includes(name));
    const fallbackPhotoId = matchedFallbackName ? FALLBACK_PHOTO_ID_BY_PRODUCT_NAME[matchedFallbackName] : undefined;
    const photoUrl = images[0] ? resolveMediaUrl(images[0].imageUrl) : fallbackPhotoId ? productPhotos[fallbackPhotoId] : undefined;

    const daysSinceCreated = (Date.now() - new Date(listing.createdAt).getTime()) / 86_400_000;
    const badges: Product["badges"] = [];
    if (daysSinceCreated <= 14) badges.push("new");
    if (listing.qualityGrade === "Премиум") badges.push("premium");
    if (listing.orderCount > 0 && listing.orderCount >= bestsellerThreshold) badges.push("bestseller");

    const unit = listing.unit;
    const harvestDate = listing.harvestDate ?? listing.expectedHarvestDate ?? listing.createdAt;
    const description = listing.description ?? "";

    return {
      id: listing.id,
      title: listing.title,
      titleTj: listing.titleTj ?? undefined,
      titleEn: listing.titleEn ?? undefined,
      slug: `${slugify(listing.title)}-${listing.id}`,
      categoryId: listing.categoryId,
      farmerId: listing.farmerProfileId,
      description,
      descriptionTj: listing.descriptionTj ?? undefined,
      descriptionEn: listing.descriptionEn ?? undefined,
      shortDescription: description.length > 140 ? `${description.slice(0, 140)}…` : description,
      unit,
      photoUrl,
      retailPricePerKg: listing.retailPricePerKg,
      wholesalePricePerKg: listing.wholesalePricePerKg ?? undefined,
      wholesaleMinimumQuantity: listing.wholesaleMinimumQuantity ?? undefined,
      availableQuantity: listing.availableQuantity,
      minimumOrderQuantity: listing.minimumOrderQuantity,
      harvestDate,
      freshnessDaysAgo: listing.freshnessDaysAgo,
      createdAt: listing.createdAt,
      qualityGrade: listing.qualityGrade,
      region: listing.region,
      district: listing.district,
      rating: listing.rating,
      // Отзывы привязаны к фермеру целиком (см. catalogStore.ts) — их число
      // не входит в /product-listings/search, а ProductCard/страница поиска
      // его и не показывает (только звёзды рейтинга); ProductDetails, где
      // reviewCount реально нужен, берёт товар из catalogStore, не отсюда.
      reviewCount: 0,
      viewCount: 0,
      orderCount: listing.orderCount,
      badges,
      specifications: [
        { label: "Регион", value: listing.region },
        { label: "Район", value: listing.district },
        { label: "Сорт/качество", value: listing.qualityGrade },
        { label: "Остаток", value: `${listing.availableQuantity} ${unit}` },
        { label: "Мин. объём заказа", value: `${listing.minimumOrderQuantity} ${unit}` },
      ],
      status: listing.status === ListingStatus.Active && listing.availableQuantity > 0 ? "active" : "outOfStock",
    };
  });

  return { items, totalCount: page.totalCount };
}

let regionsPromise: Promise<string[]> | null = null;
export function fetchCatalogRegions(): Promise<string[]> {
  if (!regionsPromise) {
    regionsPromise = apiGet<string[]>("/product-listings/regions").catch((err) => {
      regionsPromise = null;
      throw err;
    });
  }
  return regionsPromise;
}
