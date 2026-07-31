import { useEffect, useState } from "react";
import { Apple, Carrot, Leaf, Milk, Nut, Wheat } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import type { Category, Farmer, Product, Review } from "@/types";
import { apiGet, hasAuthToken, resolveMediaUrl } from "@/lib/api";
import { slugify } from "@/lib/utils";
import { productPhotos } from "@/assets/photos";
import { FarmerVerificationStatus, ListingStatus } from "@/data/farmer";
import { FALLBACK_PHOTO_ID_BY_PRODUCT_NAME } from "@/data/productPhotoFallback";

// Единый реальный источник каталога (категории/товары/фермеры) для публичного
// сайта — раньше все три были захардкожены в data/{products,farmers,categories}.ts.
// Собирается один раз (см. ensureLoaded) из уже существующих бэкенд-эндпоинтов
// (ни один не создавался специально под это) и раздаётся через простой
// pub/sub-кэш — так все страницы и хуки (useProducts/useFarmers/useCategories,
// getProductById и т.д.) сохраняют свою прежнюю синхронную сигнатуру, не
// требуя переписывать каждого потребителя под async/loading-состояние.

interface RawCategory {
  id: number;
  name: string;
  nameTj: string | null;
  nameEn: string | null;
  description: string | null;
  imageUrl: string | null;
  isActive: boolean;
}

interface RawListing {
  id: number;
  farmerProfileId: number;
  categoryId: number;
  unit: string;
  title: string;
  description: string | null;
  retailPricePerKg: number;
  wholesalePricePerKg: number | null;
  wholesaleMinimumQuantity: number | null;
  availableQuantity: number;
  minimumOrderQuantity: number;
  harvestDate: string | null;
  expectedHarvestDate: string | null;
  qualityGrade: string;
  region: string;
  district: string;
  status: number;
  createdAt: string;
}

interface RawImage {
  productListingId: number;
  imageUrl: string;
  isMain: boolean;
}

interface RawFarmerProfile {
  id: number;
  userId: number;
  farmName: string;
  region: string;
  district: string;
  village: string;
  description: string | null;
  avatarUrl: string | null;
  verificationStatus: number;
  createdAt: string;
}

interface RawReview {
  id: number;
  farmerId: number;
  customerFullName: string | null;
  rating: number;
  comment: string | null;
  createdAt: string;
}

interface RawOrderItem {
  productListingId: number;
}

interface RawPagedResult<T> {
  items: T[];
}

const CATEGORY_ICON_BY_NAME: Record<string, LucideIcon> = {
  "Овощи": Carrot,
  "Фрукты": Apple,
  "Зелень": Leaf,
  "Сухофрукты": Wheat,
  "Орехи": Nut,
  "Молочная продукция": Milk,
};

const CATEGORY_PHOTO_KEY_BY_NAME: Record<string, string> = {
  "Овощи": "vegetables",
  "Фрукты": "fruits",
  "Зелень": "greens",
  "Сухофрукты": "dried",
  "Орехи": "nuts",
  "Молочная продукция": "dairy",
};

// Категории, которых не было среди изначальных 6 (админ добавил новую,
// например "Скот") — своей фотографии-заглушки для них не нарисовано, так что
// раньше ВСЕ они получали один и тот же "vegetables" и выглядели как дубликат
// уже показанной категории рядом. Пока админ не задаст своё фото (см.
// imageUrl ниже), детерминированно раздаём один из существующих 6 вариантов
// по id — не идеально тематически, зато не повторяет соседнюю карточку 1-в-1.
const FALLBACK_PHOTO_KEYS = ["vegetables", "fruits", "greens", "dried", "nuts", "dairy"];

interface CatalogData {
  categories: Category[];
  farmers: Farmer[];
  products: Product[];
  reviewsByFarmerId: Map<number, Review[]>;
}

const EMPTY: CatalogData = { categories: [], farmers: [], products: [], reviewsByFarmerId: new Map() };

let cache: CatalogData = EMPTY;
let loadPromise: Promise<CatalogData> | null = null;
const listeners = new Set<() => void>();

async function fetchOrderCounts(): Promise<Map<number, number>> {
  // /api/order-items требует входа (история заказов — не публичные данные) —
  // гостю недоступно. Без токена даже не пытаемся — запрос гарантированно
  // получит 401 (это подтверждено живым прогоном 2026-07-31), а до фикса
  // просто ловился как ошибка, засоряя консоль на каждой гостевой странице.
  // Честно считаем orderCount нулевым, а не подделываем: бейдж "Хит продаж"
  // тогда просто не показывается.
  if (!hasAuthToken()) return new Map();

  try {
    const items = await apiGet<RawOrderItem[]>("/order-items");
    const counts = new Map<number, number>();
    for (const item of items) {
      counts.set(item.productListingId, (counts.get(item.productListingId) ?? 0) + 1);
    }
    return counts;
  } catch {
    return new Map();
  }
}

async function loadCatalog(): Promise<CatalogData> {
  const [rawCategories, listingsPage, rawImages, rawFarmerProfiles, rawReviews, orderCounts] = await Promise.all([
    apiGet<RawCategory[]>("/categories"),
    apiGet<RawPagedResult<RawListing>>("/product-listings?pageNumber=1&pageSize=200"),
    apiGet<RawImage[]>("/product-images"),
    apiGet<RawFarmerProfile[]>("/farmer-profiles"),
    apiGet<RawReview[]>("/reviews"),
    fetchOrderCounts(),
  ]);

  const activeCategories = rawCategories.filter((c) => c.isActive);

  const imagesByListingId = new Map<number, RawImage[]>();
  for (const img of rawImages) {
    const list = imagesByListingId.get(img.productListingId) ?? [];
    list.push(img);
    imagesByListingId.set(img.productListingId, list);
  }

  // Публичная витрина показывает только подтверждённых фермеров и активные
  // объявления — то же самое покупатель увидел бы в реальном магазине.
  const verifiedFarmers = rawFarmerProfiles.filter((f) => f.verificationStatus === FarmerVerificationStatus.Verified);
  const verifiedFarmerIds = new Set(verifiedFarmers.map((f) => f.id));
  const activeListings = listingsPage.items.filter(
    (l) => l.status === ListingStatus.Active && verifiedFarmerIds.has(l.farmerProfileId),
  );

  // Review привязан к фермеру целиком, не к конкретному объявлению (в
  // бэкенде нет per-товар отзывов) — рейтинг и список отзывов "о товаре" на
  // самом деле рейтинг и отзывы о фермере, который его продаёт.
  const rawReviewsByFarmerId = new Map<number, RawReview[]>();
  for (const r of rawReviews) {
    const list = rawReviewsByFarmerId.get(r.farmerId) ?? [];
    list.push(r);
    rawReviewsByFarmerId.set(r.farmerId, list);
  }
  const ratingFor = (farmerId: number) => {
    const list = rawReviewsByFarmerId.get(farmerId) ?? [];
    if (list.length === 0) return { rating: 0, reviewCount: 0 };
    const sum = list.reduce((s, r) => s + r.rating, 0);
    return { rating: Math.round((sum / list.length) * 10) / 10, reviewCount: list.length };
  };

  // CreateReviewDto требует OrderId (раздел 8.13 ТЗ) — отзыв физически не
  // может существовать без завершённого заказа, поэтому verifiedPurchase для
  // реальных отзывов всегда true, это не выдумка. Имя покупателя теперь
  // настоящее (по прямому запросу пользователя 2026-07-31 — отзыв публичный,
  // покупатель сам согласился, что его видно всем, включая фермера) — сервер
  // резолвит его через CustomerProfile→User (см. ReviewService.ToGetDto).
  const reviewsByFarmerId = new Map<number, Review[]>(
    Array.from(rawReviewsByFarmerId.entries()).map(([farmerId, list]) => [
      farmerId,
      list
        .map((r) => ({
          id: r.id,
          productId: 0,
          customerName: r.customerFullName ?? "Покупатель Market.tj",
          rating: r.rating,
          comment: r.comment ?? "",
          createdAt: r.createdAt,
          verifiedPurchase: true,
          helpfulCount: 0,
        }))
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()),
    ]),
  );

  const listingCountByFarmerId = new Map<number, number>();
  const categoryNamesByFarmerId = new Map<number, Set<string>>();
  for (const listing of activeListings) {
    listingCountByFarmerId.set(listing.farmerProfileId, (listingCountByFarmerId.get(listing.farmerProfileId) ?? 0) + 1);
    const category = activeCategories.find((c) => c.id === listing.categoryId);
    if (category) {
      const set = categoryNamesByFarmerId.get(listing.farmerProfileId) ?? new Set<string>();
      set.add(category.name);
      categoryNamesByFarmerId.set(listing.farmerProfileId, set);
    }
  }

  const categories: Category[] = activeCategories.map((c) => ({
    id: c.id,
    slug: String(c.id),
    name: c.name,
    nameTj: c.nameTj ?? undefined,
    nameEn: c.nameEn ?? undefined,
    description: c.description ?? "",
    icon: CATEGORY_ICON_BY_NAME[c.name] ?? Leaf,
    photoKey: CATEGORY_PHOTO_KEY_BY_NAME[c.name] ?? FALLBACK_PHOTO_KEYS[c.id % FALLBACK_PHOTO_KEYS.length],
    imageUrl: c.imageUrl,
    productCount: activeListings.filter((l) => l.categoryId === c.id).length,
  }));

  const farmers: Farmer[] = verifiedFarmers.map((f) => {
    const { rating, reviewCount } = ratingFor(f.id);
    return {
      id: f.id,
      userId: f.userId,
      farmName: f.farmName,
      // Полное имя владельца — это User.FullName, а публичные эндпоинты
      // каталога не отдают чужие персональные данные (UserController — только
      // для Admin). Название хозяйства и так публичная витринная identity.
      ownerName: f.farmName,
      avatarUrl: f.avatarUrl ?? undefined,
      region: f.region,
      district: f.district,
      village: f.village,
      bio: f.description ?? "",
      story: f.description ?? "",
      rating,
      reviewCount,
      productCount: listingCountByFarmerId.get(f.id) ?? 0,
      yearsActive: Math.max(0, new Date().getFullYear() - new Date(f.createdAt).getFullYear()),
      verified: true,
      tags: Array.from(categoryNamesByFarmerId.get(f.id) ?? []),
      joinedAt: f.createdAt,
    };
  });
  const farmerById = new Map(farmers.map((f) => [f.id, f]));

  const orderCountValues = activeListings.map((l) => orderCounts.get(l.id) ?? 0).sort((a, b) => b - a);
  const bestsellerRank = Math.max(0, Math.ceil(orderCountValues.length * 0.2) - 1);
  const bestsellerThreshold = orderCountValues[bestsellerRank] ?? 0;

  const products: Product[] = activeListings.map((listing) => {
    const farmer = farmerById.get(listing.farmerProfileId);
    const images = [...(imagesByListingId.get(listing.id) ?? [])].sort((a, b) => Number(b.isMain) - Number(a.isMain));
    // Название теперь свободно вводит фермер (см. миграцию
    // AddCategoryAndUnitToProductListing) — сопоставляем с известными базовыми
    // названиями по вхождению подстроки в Title, как уже делает
    // fallbackPhotoByTitle в data/farmer.ts для того же случая.
    const matchedFallbackName = Object.keys(FALLBACK_PHOTO_ID_BY_PRODUCT_NAME).find((name) => listing.title.includes(name));
    const fallbackPhotoId = matchedFallbackName ? FALLBACK_PHOTO_ID_BY_PRODUCT_NAME[matchedFallbackName] : undefined;
    const photoUrl = images[0] ? resolveMediaUrl(images[0].imageUrl) : fallbackPhotoId ? productPhotos[fallbackPhotoId] : undefined;

    const orderCount = orderCounts.get(listing.id) ?? 0;
    const daysSinceCreated = (Date.now() - new Date(listing.createdAt).getTime()) / 86_400_000;

    const badges: Product["badges"] = [];
    if (daysSinceCreated <= 14) badges.push("new");
    if (listing.qualityGrade === "Премиум") badges.push("premium");
    if (orderCount > 0 && orderCount >= bestsellerThreshold) badges.push("bestseller");

    const unit = listing.unit;
    const harvestDate = listing.harvestDate ?? listing.expectedHarvestDate ?? listing.createdAt;
    const description = listing.description ?? "";

    return {
      id: listing.id,
      title: listing.title,
      slug: `${slugify(listing.title)}-${listing.id}`,
      categoryId: listing.categoryId,
      farmerId: listing.farmerProfileId,
      description,
      shortDescription: description.length > 140 ? `${description.slice(0, 140)}…` : description,
      unit,
      photoUrl,
      retailPricePerKg: listing.retailPricePerKg,
      wholesalePricePerKg: listing.wholesalePricePerKg ?? undefined,
      wholesaleMinimumQuantity: listing.wholesaleMinimumQuantity ?? undefined,
      availableQuantity: listing.availableQuantity,
      minimumOrderQuantity: listing.minimumOrderQuantity,
      harvestDate,
      createdAt: listing.createdAt,
      qualityGrade: listing.qualityGrade,
      region: listing.region,
      district: listing.district,
      rating: farmer?.rating ?? 0,
      reviewCount: farmer?.reviewCount ?? 0,
      viewCount: 0,
      orderCount,
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

  return { categories, farmers, products, reviewsByFarmerId };
}

let loaded = false;

function notify() {
  listeners.forEach((listener) => listener());
}

function ensureLoaded(): void {
  if (loadPromise) return;
  loadPromise = loadCatalog()
    .then((data) => {
      cache = data;
      loaded = true;
      notify();
      return data;
    })
    .catch((err) => {
      loadPromise = null; // разрешаем повторную попытку при следующем обращении
      throw err;
    });
}

// Пока первая загрузка не завершилась, cache пуст — это позволяет страницам,
// которые иначе приняли бы пустой массив за "ничего не найдено" (например,
// ProductDetails по несуществующему slug), на секунду показать честный
// лоадер вместо ложного "товар не найден".
export function useCatalogLoaded(): boolean {
  const [, forceRender] = useState(0);
  useEffect(() => {
    const listener = () => forceRender((n) => n + 1);
    listeners.add(listener);
    ensureLoaded();
    return () => {
      listeners.delete(listener);
    };
  }, []);
  return loaded;
}

function useCatalogSlice<T>(select: (data: CatalogData) => T): T {
  const [, forceRender] = useState(0);
  useEffect(() => {
    const listener = () => forceRender((n) => n + 1);
    listeners.add(listener);
    ensureLoaded();
    return () => {
      listeners.delete(listener);
    };
  }, []);
  return select(cache);
}

export function useCatalogProducts(): Product[] {
  return useCatalogSlice((data) => data.products);
}

export function useCatalogFarmers(): Farmer[] {
  return useCatalogSlice((data) => data.farmers);
}

export function useCatalogCategories(): Category[] {
  return useCatalogSlice((data) => data.categories);
}

export function getCatalogProductById(id: number): Product | undefined {
  return cache.products.find((p) => p.id === id);
}

export function getCatalogFarmerById(id: number): Farmer | undefined {
  return cache.farmers.find((f) => f.id === id);
}

export function getCatalogReviewsByFarmerId(farmerId: number): Review[] {
  return cache.reviewsByFarmerId.get(farmerId) ?? [];
}
