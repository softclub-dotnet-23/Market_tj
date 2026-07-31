import { useEffect, useMemo, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { Heart, PackageSearch, Search, SlidersHorizontal, X } from "lucide-react";
import { Breadcrumbs } from "@/components/ui/Breadcrumbs";
import { Dropdown } from "@/components/ui/Dropdown";
import { Chip } from "@/components/ui/Chip";
import { Pagination } from "@/components/ui/Pagination";
import { EmptyState } from "@/components/ui/EmptyState";
import { ProductCardSkeleton } from "@/components/ui/Skeleton";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";
import { ProductCard } from "@/components/product/ProductCard";
import { CatalogFilters, type CatalogFilterState } from "@/components/product/CatalogFilters";
import { useCategories } from "@/data/categories";
import { useFarmers } from "@/data/farmers";
import { useFavorites } from "@/context/FavoritesContext";
import { searchCatalog, fetchCatalogRegions } from "@/data/catalogSearch";
import type { Product } from "@/types";

const PAGE_SIZE = 12;

export function Catalog() {
  const { t } = useTranslation(["pages", "layout", "product", "common", "data"]);
  const SORT_OPTIONS = [
    { value: "popularity", label: t("pages:catalog.sortPopularity") },
    { value: "price-asc", label: t("pages:catalog.sortPriceAsc") },
    { value: "price-desc", label: t("pages:catalog.sortPriceDesc") },
    { value: "rating", label: t("pages:catalog.sortRating") },
    { value: "fresh", label: t("pages:catalog.sortFresh") },
  ];
  const farmers = useFarmers();
  const categories = useCategories();
  const [searchParams, setSearchParams] = useSearchParams();
  const [mobileFiltersOpen, setMobileFiltersOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [items, setItems] = useState<Product[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const { favoriteIds } = useFavorites();
  const resultsTopRef = useRef<HTMLDivElement>(null);
  const isFirstPageRender = useRef(true);

  // Раздел 13.5 ТЗ: регионы — реально встречающиеся значения среди видимых
  // объявлений, отдаёт бэкенд (GET /product-listings/regions), а не
  // Set(...) поверх уже скачанного полного списка товаров, как раньше.
  const allRegionsLabel = t("pages:catalog.allRegions");
  const [backendRegions, setBackendRegions] = useState<string[]>([]);
  useEffect(() => {
    let cancelled = false;
    fetchCatalogRegions()
      .then((list) => {
        if (!cancelled) setBackendRegions(list);
      })
      .catch(() => {
        if (!cancelled) setBackendRegions([]);
      });
    return () => {
      cancelled = true;
    };
  }, []);
  const regions = useMemo(() => [allRegionsLabel, ...backendRegions], [allRegionsLabel, backendRegions]);

  const search = searchParams.get("search") ?? "";
  const [searchInput, setSearchInput] = useState(search);
  const categorySlugs = useMemo(
    () => (searchParams.get("category") ? searchParams.get("category")!.split(",") : []),
    [searchParams],
  );
  // regions[0] === allRegionsLabel всегда (см. useMemo выше) — сравниваем
  // напрямую с allRegionsLabel, а не с regions[0], чтобы поисковый эффект
  // ниже не зависел от массива regions целиком (тот меняет identity, как
  // только приходит ответ /product-listings/regions, и заново перевызывал
  // бы поиск без реальной смены значения фильтра).
  const region = searchParams.get("region") ?? allRegionsLabel;
  const farmerId = searchParams.get("farmer") ? Number(searchParams.get("farmer")) : null;
  const priceMin = searchParams.get("minPrice") ?? "";
  const priceMax = searchParams.get("maxPrice") ?? "";
  const sortBy = searchParams.get("sortBy") ?? "popularity";
  const favoritesOnly = searchParams.get("favorites") === "1";
  const page = Number(searchParams.get("page") ?? "1");

  useEffect(() => setSearchInput(search), [search]);

  const updateParams = (patch: Record<string, string | null>, resetPage = true, replace = false) => {
    const next = new URLSearchParams(searchParams);
    Object.entries(patch).forEach(([key, value]) => {
      if (value === null || value === "") next.delete(key);
      else next.set(key, value);
    });
    if (resetPage) next.delete("page");
    setSearchParams(next, { replace });
  };

  // Поиск применяется "вживую" по мере ввода (с небольшой задержкой), а не
  // только по Enter — раньше стирание текста в поле никак не сбрасывало сам
  // фильтр (search в URL менялся только через submit формы), и товары
  // оставались отфильтрованы по уже стёртому запросу, хотя поле выглядело
  // пустым. replace: true — чтобы каждая буква при вводе не плодила запись в
  // истории браузера (иначе "Назад" пришлось бы жать посимвольно).
  useEffect(() => {
    const timer = setTimeout(() => {
      updateParams({ search: searchInput || null }, true, true);
    }, 300);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchInput]);

  const filterState: CatalogFilterState = { categorySlugs, region, priceMin, priceMax };

  const handleFilterChange = (patch: Partial<CatalogFilterState>) => {
    const nextPatch: Record<string, string | null> = {};
    if (patch.categorySlugs) nextPatch.category = patch.categorySlugs.length ? patch.categorySlugs.join(",") : null;
    if (patch.region !== undefined) nextPatch.region = patch.region === regions[0] ? null : patch.region;
    if (patch.priceMin !== undefined) nextPatch.minPrice = patch.priceMin || null;
    if (patch.priceMax !== undefined) nextPatch.maxPrice = patch.priceMax || null;
    updateParams(nextPatch);
  };

  const resetFilters = () => {
    setSearchParams(new URLSearchParams(favoritesOnly ? { favorites: "1" } : {}));
    setSearchInput("");
  };

  // Раздел 13.5 ТЗ: сам поиск/фильтр/сортировка/пагинация выполняются на
  // бэкенде (GET /product-listings/search) — здесь только собираем параметры
  // из состояния URL и отправляем запрос, вместо client-side .filter()/.sort()
  // поверх заранее выгруженного полного списка товаров.
  useEffect(() => {
    // slug категории — это её id (см. catalogStore.ts: slug: String(c.id)),
    // конвертация не требует поиска по массиву categories — это важно, иначе
    // пришлось бы держать нестабильный (каждый рендер новый) объект
    // categories в зависимостях эффекта и слать лишние запросы.
    const categoryIds = categorySlugs.map(Number).filter((id) => !Number.isNaN(id));

    // Избранное — чисто клиентский localStorage (см. FavoritesContext), не
    // таблица в БД, поэтому фильтр "только избранное" передаётся как
    // конкретный список id. Если избранного нет вообще — сети можно не
    // дожидаться, результат заведомо пуст (backend трактует пустой
    // listingIds как "фильтр не применён", что дало бы ложно ВСЕ товары).
    if (favoritesOnly && favoriteIds.length === 0) {
      setItems([]);
      setTotalCount(0);
      setLoading(false);
      return;
    }

    let cancelled = false;
    setLoading(true);
    searchCatalog({
      pageNumber: page,
      pageSize: PAGE_SIZE,
      categoryIds: categoryIds.length ? categoryIds : undefined,
      region: region !== allRegionsLabel ? region : undefined,
      farmerId: farmerId || undefined,
      priceMin: priceMin ? Number(priceMin) : undefined,
      priceMax: priceMax ? Number(priceMax) : undefined,
      search: search || undefined,
      sortBy,
      listingIds: favoritesOnly ? favoriteIds : undefined,
    })
      .then((result) => {
        if (cancelled) return;
        setItems(result.items);
        setTotalCount(result.totalCount);
      })
      .catch(() => {
        if (cancelled) return;
        setItems([]);
        setTotalCount(0);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search, categorySlugs.join(), region, allRegionsLabel, farmerId, priceMin, priceMax, sortBy, page, favoritesOnly, favoriteIds.join()]);

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);

  useEffect(() => {
    // Only for page-number changes (not the initial mount) — gently bring the
    // fresh results into view instead of leaving the reader stranded down by
    // the pagination controls while new cards silently swap in above them.
    if (isFirstPageRender.current) {
      isFirstPageRender.current = false;
      return;
    }
    resultsTopRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, [currentPage]);

  const activeChips: { key: string; label: string; onRemove: () => void }[] = [];
  categorySlugs.forEach((slug) => {
    const c = categories.find((cat) => cat.slug === slug);
    if (c) activeChips.push({ key: `cat-${slug}`, label: c.name, onRemove: () => handleFilterChange({ categorySlugs: categorySlugs.filter((s) => s !== slug) }) });
  });
  if (region !== regions[0]) activeChips.push({ key: "region", label: region, onRemove: () => handleFilterChange({ region: regions[0] }) });
  if (farmerId) {
    const f = farmers.find((farmer) => farmer.id === farmerId);
    if (f) activeChips.push({ key: "farmer", label: f.farmName, onRemove: () => updateParams({ farmer: null }) });
  }
  if (priceMin || priceMax) activeChips.push({ key: "price", label: `${priceMin || 0}–${priceMax || "∞"} ${t("common:currencySomoni")}`, onRemove: () => handleFilterChange({ priceMin: "", priceMax: "" }) });
  if (favoritesOnly) activeChips.push({ key: "fav", label: t("common:actions.favorites"), onRemove: () => updateParams({ favorites: null }) });

  return (
    <div className="container-page py-8 sm:py-12">
      <Breadcrumbs items={[{ label: t("layout:nav.catalog") }]} className="mb-6" />

      <div className="mb-8 flex flex-col gap-3">
        <h1 className="font-display text-3xl text-stone-900 sm:text-4xl dark:text-stone-50">
          {favoritesOnly ? t("pages:catalog.favoritesTitle") : t("pages:catalog.title")}
        </h1>
        <p className="text-stone-500 dark:text-stone-400">
          {loading ? t("pages:catalog.searching") : t("pages:catalog.foundCount", { count: totalCount })}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-10 lg:grid-cols-[260px_1fr]">
        <aside className="hidden lg:block">
          {/* max-h+overflow — иначе "прилипшую" панель выше видимой области
              нельзя докрутить: sticky фиксирует её позицию, а не размер, и
              нижние поля (цена, "Сбросить") становились недостижимы на
              невысоких экранах. Теперь панель скроллится сама в себе. */}
          <div className="sticky top-24 max-h-[calc(100vh-7rem)] overflow-y-auto rounded-3xl border border-stone-100 bg-white p-6 dark:border-stone-800 dark:bg-stone-900">
            <CatalogFilters state={filterState} regions={regions} onChange={handleFilterChange} onReset={resetFilters} />
          </div>
        </aside>

        <div ref={resultsTopRef} className="scroll-mt-24">
          <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <form
              onSubmit={(e) => {
                e.preventDefault();
                updateParams({ search: searchInput || null });
              }}
              className="flex h-12 items-center gap-2 rounded-xl border border-stone-200 bg-white px-4 sm:max-w-xs sm:flex-1 dark:border-stone-700 dark:bg-stone-900"
            >
              <Search size={16} className="shrink-0 text-stone-400 dark:text-stone-500" />
              <input
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder={t("pages:catalog.searchPlaceholder")}
                className="w-full bg-transparent text-sm outline-none placeholder:text-stone-400 dark:text-stone-100 dark:placeholder:text-stone-500"
              />
              {searchInput && (
                <button type="button" onClick={() => { setSearchInput(""); updateParams({ search: null }); }}>
                  <X size={14} className="text-stone-400 dark:text-stone-500" />
                </button>
              )}
            </form>

            <div className="flex items-center gap-2.5">
              <button
                onClick={() => setMobileFiltersOpen(true)}
                className="flex h-12 items-center gap-2 rounded-xl border border-stone-200 bg-white px-4 text-sm font-medium text-stone-700 lg:hidden dark:border-stone-700 dark:bg-stone-900 dark:text-stone-200"
              >
                <SlidersHorizontal size={15} />
                {t("product:filters.title")}
                {activeChips.length > 0 && (
                  <span className="flex h-5 w-5 items-center justify-center rounded-full bg-grove-700 text-[10px] font-bold text-white">
                    {activeChips.length}
                  </span>
                )}
              </button>
              <Dropdown
                options={SORT_OPTIONS}
                value={sortBy}
                onChange={(v) => updateParams({ sortBy: v === "popularity" ? null : v })}
                className="w-52"
              />
            </div>
          </div>

          {activeChips.length > 0 && (
            <div className="mb-6 flex flex-wrap gap-2">
              {activeChips.map((chip) => (
                <Chip key={chip.key} active onRemove={chip.onRemove}>
                  {chip.label}
                </Chip>
              ))}
            </div>
          )}

          {loading ? (
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 sm:gap-5 xl:grid-cols-4">
              {Array.from({ length: items.length || PAGE_SIZE }).map((_, i) => (
                <ProductCardSkeleton key={i} />
              ))}
            </div>
          ) : items.length === 0 ? (
            <EmptyState
              icon={favoritesOnly ? <Heart size={26} /> : <PackageSearch size={26} />}
              title={favoritesOnly ? t("pages:catalog.emptyFavoritesTitle") : t("pages:catalog.emptyResultsTitle")}
              description={
                favoritesOnly
                  ? t("pages:catalog.emptyFavoritesDescription")
                  : t("pages:catalog.emptyResultsDescription")
              }
              action={
                <Button variant="outline" onClick={resetFilters}>
                  {t("product:filters.resetFilters")}
                </Button>
              }
            />
          ) : (
            <motion.div
              initial="hidden"
              animate="visible"
              variants={{ visible: { transition: { staggerChildren: 0.04 } } }}
              className="grid grid-cols-2 gap-4 sm:grid-cols-3 sm:gap-5 xl:grid-cols-4"
            >
              {items.map((product) => (
                <motion.div
                  key={product.id}
                  variants={{ hidden: { opacity: 0, y: 16 }, visible: { opacity: 1, y: 0 } }}
                >
                  <ProductCard product={product} />
                </motion.div>
              ))}
            </motion.div>
          )}

          {!loading && items.length > 0 && (
            <Pagination
              page={currentPage}
              totalPages={totalPages}
              onPageChange={(p) => updateParams({ page: String(p) }, false)}
              className="mt-12"
            />
          )}
        </div>
      </div>

      <Modal open={mobileFiltersOpen} onClose={() => setMobileFiltersOpen(false)} className="max-w-sm">
        <CatalogFilters
          state={filterState}
          regions={regions}
          onChange={handleFilterChange}
          onReset={() => {
            resetFilters();
            setMobileFiltersOpen(false);
          }}
        />
        <Button className="mt-6 w-full" onClick={() => setMobileFiltersOpen(false)}>
          {t("pages:catalog.showCount", { count: totalCount })}
        </Button>
      </Modal>
    </div>
  );
}
