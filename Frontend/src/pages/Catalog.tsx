import { useEffect, useMemo, useState } from "react";
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
import { useProducts } from "@/data/products";
import { useCategories } from "@/data/categories";
import { useFarmers } from "@/data/farmers";
import { regions } from "@/data/site";
import { useFavorites } from "@/context/FavoritesContext";

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
  const products = useProducts();
  const [searchParams, setSearchParams] = useSearchParams();
  const [mobileFiltersOpen, setMobileFiltersOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const { favoriteIds } = useFavorites();
  const categories = useCategories();

  const search = searchParams.get("search") ?? "";
  const [searchInput, setSearchInput] = useState(search);
  const categorySlugs = useMemo(
    () => (searchParams.get("category") ? searchParams.get("category")!.split(",") : []),
    [searchParams],
  );
  const region = searchParams.get("region") ?? regions[0];
  const farmerId = searchParams.get("farmer") ? Number(searchParams.get("farmer")) : null;
  const onlyAvailable = searchParams.get("available") === "1";
  const priceMin = searchParams.get("minPrice") ?? "";
  const priceMax = searchParams.get("maxPrice") ?? "";
  const sortBy = searchParams.get("sortBy") ?? "popularity";
  const favoritesOnly = searchParams.get("favorites") === "1";
  const page = Number(searchParams.get("page") ?? "1");

  useEffect(() => setSearchInput(search), [search]);

  const updateParams = (patch: Record<string, string | null>, resetPage = true) => {
    const next = new URLSearchParams(searchParams);
    Object.entries(patch).forEach(([key, value]) => {
      if (value === null || value === "") next.delete(key);
      else next.set(key, value);
    });
    if (resetPage) next.delete("page");
    setSearchParams(next, { replace: false });
  };

  const filterState: CatalogFilterState = { categorySlugs, region, onlyAvailable, priceMin, priceMax };

  const handleFilterChange = (patch: Partial<CatalogFilterState>) => {
    const nextPatch: Record<string, string | null> = {};
    if (patch.categorySlugs) nextPatch.category = patch.categorySlugs.length ? patch.categorySlugs.join(",") : null;
    if (patch.region !== undefined) nextPatch.region = patch.region === regions[0] ? null : patch.region;
    if (patch.onlyAvailable !== undefined) nextPatch.available = patch.onlyAvailable ? "1" : null;
    if (patch.priceMin !== undefined) nextPatch.minPrice = patch.priceMin || null;
    if (patch.priceMax !== undefined) nextPatch.maxPrice = patch.priceMax || null;
    updateParams(nextPatch);
  };

  const resetFilters = () => {
    setSearchParams(new URLSearchParams(favoritesOnly ? { favorites: "1" } : {}));
    setSearchInput("");
  };

  const filtered = useMemo(() => {
    let list = [...products];
    if (favoritesOnly) list = list.filter((p) => favoriteIds.includes(p.id));
    if (search) {
      const q = search.toLowerCase();
      list = list.filter((p) => p.title.toLowerCase().includes(q) || p.shortDescription.toLowerCase().includes(q));
    }
    if (categorySlugs.length) {
      const ids = categorySlugs.map((s) => categories.find((c) => c.slug === s)?.id).filter(Boolean);
      list = list.filter((p) => ids.includes(p.categoryId));
    }
    if (region !== regions[0]) list = list.filter((p) => p.region === region);
    if (farmerId) list = list.filter((p) => p.farmerId === farmerId);
    if (onlyAvailable) list = list.filter((p) => p.status === "active" && p.availableQuantity > 0);
    if (priceMin) list = list.filter((p) => p.retailPricePerKg >= Number(priceMin));
    if (priceMax) list = list.filter((p) => p.retailPricePerKg <= Number(priceMax));

    switch (sortBy) {
      case "price-asc":
        list.sort((a, b) => a.retailPricePerKg - b.retailPricePerKg);
        break;
      case "price-desc":
        list.sort((a, b) => b.retailPricePerKg - a.retailPricePerKg);
        break;
      case "rating":
        list.sort((a, b) => b.rating - a.rating);
        break;
      case "fresh":
        list.sort((a, b) => new Date(b.harvestDate).getTime() - new Date(a.harvestDate).getTime());
        break;
      default:
        list.sort((a, b) => b.orderCount - a.orderCount);
    }
    return list;
  }, [search, categorySlugs, region, farmerId, onlyAvailable, priceMin, priceMax, sortBy, favoritesOnly, favoriteIds, categories, products]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems = filtered.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  useEffect(() => {
    setLoading(true);
    const t = setTimeout(() => setLoading(false), 420);
    return () => clearTimeout(t);
  }, [search, categorySlugs.join(), region, farmerId, onlyAvailable, priceMin, priceMax, sortBy, currentPage, favoritesOnly]);

  const activeChips: { key: string; label: string; onRemove: () => void }[] = [];
  categorySlugs.forEach((slug) => {
    const c = categories.find((cat) => cat.slug === slug);
    if (c) activeChips.push({ key: `cat-${slug}`, label: c.name, onRemove: () => handleFilterChange({ categorySlugs: categorySlugs.filter((s) => s !== slug) }) });
  });
  if (region !== regions[0]) activeChips.push({ key: "region", label: t(`data:regionLabels.${region}`), onRemove: () => handleFilterChange({ region: regions[0] }) });
  if (farmerId) {
    const f = farmers.find((farmer) => farmer.id === farmerId);
    if (f) activeChips.push({ key: "farmer", label: f.farmName, onRemove: () => updateParams({ farmer: null }) });
  }
  if (onlyAvailable) activeChips.push({ key: "avail", label: t("product:filters.onlyAvailable"), onRemove: () => handleFilterChange({ onlyAvailable: false }) });
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
          {loading ? t("pages:catalog.searching") : t("pages:catalog.foundCount", { count: filtered.length })}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-10 lg:grid-cols-[260px_1fr]">
        <aside className="hidden lg:block">
          <div className="sticky top-24 rounded-3xl border border-stone-100 bg-white p-6 dark:border-stone-800 dark:bg-stone-900">
            <CatalogFilters state={filterState} onChange={handleFilterChange} onReset={resetFilters} />
          </div>
        </aside>

        <div>
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
              {Array.from({ length: 8 }).map((_, i) => (
                <ProductCardSkeleton key={i} />
              ))}
            </div>
          ) : pageItems.length === 0 ? (
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
              {pageItems.map((product) => (
                <motion.div
                  key={product.id}
                  variants={{ hidden: { opacity: 0, y: 16 }, visible: { opacity: 1, y: 0 } }}
                >
                  <ProductCard product={product} />
                </motion.div>
              ))}
            </motion.div>
          )}

          {!loading && pageItems.length > 0 && (
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
          onChange={handleFilterChange}
          onReset={() => {
            resetFilters();
            setMobileFiltersOpen(false);
          }}
        />
        <Button className="mt-6 w-full" onClick={() => setMobileFiltersOpen(false)}>
          {t("pages:catalog.showCount", { count: filtered.length })}
        </Button>
      </Modal>
    </div>
  );
}
