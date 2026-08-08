import { useEffect, useState } from "react";
import { Apple, Carrot, Leaf, Milk, Nut, Wheat } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import type { Category, Farmer, Product, Review } from "@/types";
import { apiGet, resolveMediaUrl } from "@/lib/api";
import { slugify } from "@/lib/utils";
import { productPhotos } from "@/assets/photos";
import { ListingStatus } from "@/data/farmer";
import { FALLBACK_PHOTO_ID_BY_PRODUCT_NAME } from "@/data/productPhotoFallback";

// Единый реальный источник каталога (категории/товары/фермеры) для публичного
// сайта — раньше все три были захардкожены в data/{products,farmers,categories}.ts.
// Собирается один раз (см. ensureLoaded) из уже существующих бэкенд-эндпоинтов
// (ни один не создавался специально под это) и раздаётся через простой
// pub/sub-кэш — так все страницы и хуки (useProducts/useFarmers/useCategories,
// getProductById и т.д.) сохраняют свою прежнюю синхронную сигнатуру, не
// требуя переписывать каждого потребителя под async/loading-состояние.
//
// Audit 2026-08-02: раньше здесь тянулись /product-images и /reviews ЦЕЛИКОМ
// (все картинки и все отзывы сайта на каждую полную загрузку приложения) и
// verified-фильтр/рейтинг/теги фермера считались в браузере. Теперь картинки,
// рейтинг и число заказов приходят прямо на каждом объявлении с
// /product-listings, а фермеры (уже отфильтрованные по Verified, с готовыми
// рейтингом/числом отзывов/товаров/тегами) — с /farmer-profiles/public.
// Отзывы по-прежнему нужны текстом (не только агрегатом) для витрины
// фермера/товара — берутся отдельным запросом на каждого подтверждённого
// фермера (/reviews?farmerId=X), а не одним неограниченным /reviews на весь
// сайт.

interface RawCategory {
  id: number;
  name: string;
  nameTj: string | null;
  nameEn: string | null;
  description: string | null;
  imageUrl: string | null;
  isActive: boolean;
  activeListingCount: number;
}

interface RawListing {
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
  imageUrls: string[];
  rating: number;
  orderCount: number;
}

interface RawPublicFarmer {
  id: number;
  userId: number;
  farmName: string;
  region: string;
  district: string;
  village: string;
  description: string | null;
  avatarUrl: string | null;
  createdAt: string;
  rating: number;
  reviewCount: number;
  productCount: number;
  tags: string[];
}

interface RawReview {
  id: number;
  farmerId: number;
  customerFullName: string | null;
  rating: number;
  comment: string | null;
  createdAt: string;
  farmerReply: string | null;
  farmerRepliedAt: string | null;
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

async function loadCatalog(): Promise<CatalogData> {
  const [rawCategories, listingsPage, rawFarmers] = await Promise.all([
    apiGet<RawCategory[]>("/categories"),
    apiGet<RawPagedResult<RawListing>>("/product-listings?pageNumber=1&pageSize=200"),
    apiGet<RawPublicFarmer[]>("/farmer-profiles/public"),
  ]);

  const activeCategories = rawCategories.filter((c) => c.isActive);

  // /farmer-profiles/public уже отдаёт только подтверждённых фермеров —
  // отдельного verified-фильтра на фронте больше не нужно.
  const verifiedFarmerIds = new Set(rawFarmers.map((f) => f.id));
  const activeListings = listingsPage.items.filter(
    (l) => l.status === ListingStatus.Active && verifiedFarmerIds.has(l.farmerProfileId),
  );

  const reviewListsByFarmer = await Promise.all(
    rawFarmers.map((f) => apiGet<RawReview[]>(`/reviews?farmerId=${f.id}`).catch(() => [] as RawReview[])),
  );
  const rawReviewsByFarmerId = new Map<number, RawReview[]>(rawFarmers.map((f, i) => [f.id, reviewListsByFarmer[i]]));

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
          farmerReply: r.farmerReply ? { message: r.farmerReply, createdAt: r.farmerRepliedAt ?? r.createdAt } : undefined,
        }))
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()),
    ]),
  );

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
    productCount: c.activeListingCount,
  }));

  const farmers: Farmer[] = rawFarmers.map((f) => ({
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
    rating: f.rating,
    reviewCount: f.reviewCount,
    productCount: f.productCount,
    yearsActive: Math.max(0, new Date().getFullYear() - new Date(f.createdAt).getFullYear()),
    verified: true,
    tags: f.tags,
    joinedAt: f.createdAt,
  }));
  const farmerById = new Map(farmers.map((f) => [f.id, f]));

  const orderCountValues = activeListings.map((l) => l.orderCount).sort((a, b) => b - a);
  const bestsellerRank = Math.max(0, Math.ceil(orderCountValues.length * 0.2) - 1);
  const bestsellerThreshold = orderCountValues[bestsellerRank] ?? 0;

  const products: Product[] = activeListings.map((listing) => {
    const farmer = farmerById.get(listing.farmerProfileId);
    // Название теперь свободно вводит фермер (см. миграцию
    // AddCategoryAndUnitToProductListing) — сопоставляем с известными базовыми
    // названиями по вхождению подстроки в Title, как уже делает
    // fallbackPhotoByTitle в data/farmer.ts для того же случая.
    const matchedFallbackName = Object.keys(FALLBACK_PHOTO_ID_BY_PRODUCT_NAME).find((name) => listing.title.includes(name));
    const fallbackPhotoId = matchedFallbackName ? FALLBACK_PHOTO_ID_BY_PRODUCT_NAME[matchedFallbackName] : undefined;
    const photoUrl = listing.imageUrls[0]
      ? resolveMediaUrl(listing.imageUrls[0])
      : fallbackPhotoId
        ? productPhotos[fallbackPhotoId]
        : undefined;

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
      // Slug остаётся привязан к русскому названию всегда (не зависит от
      // языка отображения) — иначе ссылка на товар менялась бы при
      // переключении языка UI (2026-08-05).
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
      reviewCount: farmer?.reviewCount ?? 0,
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
