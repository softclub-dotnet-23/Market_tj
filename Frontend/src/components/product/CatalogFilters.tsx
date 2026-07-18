import { useTranslation } from "react-i18next";
import { Leaf } from "lucide-react";
import { useCategories } from "@/data/categories";
import { regions } from "@/data/site";
import { Checkbox } from "@/components/ui/Field";
import { Dropdown } from "@/components/ui/Dropdown";
import { Button } from "@/components/ui/Button";

export interface CatalogFilterState {
  categorySlugs: string[];
  region: string;
  onlyAvailable: boolean;
  priceMin: string;
  priceMax: string;
}

export function CatalogFilters({
  state,
  onChange,
  onReset,
}: {
  state: CatalogFilterState;
  onChange: (patch: Partial<CatalogFilterState>) => void;
  onReset: () => void;
}) {
  const { t } = useTranslation("product");
  const categories = useCategories();
  const toggleCategory = (slug: string) => {
    const next = state.categorySlugs.includes(slug)
      ? state.categorySlugs.filter((s) => s !== slug)
      : [...state.categorySlugs, slug];
    onChange({ categorySlugs: next });
  };

  return (
    <div className="flex flex-col gap-8">
      <div className="flex items-center justify-between">
        <h3 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("filters.title")}</h3>
        <button onClick={onReset} className="text-xs font-medium text-grove-700 hover:text-grove-800 dark:text-grove-400 dark:hover:text-grove-300">
          {t("filters.resetAll")}
        </button>
      </div>

      <div>
        <p className="mb-3 text-sm font-semibold text-stone-700 dark:text-stone-300">{t("filters.category")}</p>
        <div className="flex flex-col gap-2.5">
          {categories.map((c) => (
            <Checkbox
              key={c.id}
              checked={state.categorySlugs.includes(c.slug)}
              onChange={() => toggleCategory(c.slug)}
              label={
                <span className="flex flex-1 items-center justify-between gap-2">
                  <span>{c.name}</span>
                  <span className="text-xs text-stone-400 dark:text-stone-500">{c.productCount}</span>
                </span>
              }
            />
          ))}
        </div>
      </div>

      <div>
        <p className="mb-3 text-sm font-semibold text-stone-700 dark:text-stone-300">{t("filters.region")}</p>
        <Dropdown
          options={regions.map((r) => ({ value: r, label: r }))}
          value={state.region}
          onChange={(v) => onChange({ region: v })}
        />
      </div>

      <div>
        <p className="mb-3 text-sm font-semibold text-stone-700 dark:text-stone-300">{t("filters.price")}</p>
        <div className="flex items-center gap-2">
          <input
            type="number"
            min={0}
            value={state.priceMin}
            onChange={(e) => onChange({ priceMin: e.target.value })}
            placeholder={t("filters.priceFrom")}
            className="h-11 w-full rounded-xl border border-stone-200 bg-white px-3 text-sm focus:border-grove-500 focus:ring-2 focus:ring-grove-100 dark:border-stone-700 dark:bg-stone-900 dark:text-stone-100 dark:placeholder:text-stone-500 dark:focus:ring-grove-900"
          />
          <span className="text-stone-300 dark:text-stone-600">—</span>
          <input
            type="number"
            min={0}
            value={state.priceMax}
            onChange={(e) => onChange({ priceMax: e.target.value })}
            placeholder={t("filters.priceTo")}
            className="h-11 w-full rounded-xl border border-stone-200 bg-white px-3 text-sm focus:border-grove-500 focus:ring-2 focus:ring-grove-100 dark:border-stone-700 dark:bg-stone-900 dark:text-stone-100 dark:placeholder:text-stone-500 dark:focus:ring-grove-900"
          />
        </div>
      </div>

      <div className="flex items-center justify-between rounded-xl bg-stone-50 px-4 py-3.5 dark:bg-stone-800">
        <span className="flex items-center gap-2 text-sm font-medium text-stone-700 dark:text-stone-200">
          <Leaf size={15} className="text-grove-600 dark:text-grove-400" />
          {t("filters.onlyAvailable")}
        </span>
        <button
          role="switch"
          aria-checked={state.onlyAvailable}
          onClick={() => onChange({ onlyAvailable: !state.onlyAvailable })}
          className={`relative h-6 w-11 shrink-0 rounded-full transition-colors ${
            state.onlyAvailable ? "bg-grove-600" : "bg-stone-300 dark:bg-stone-600"
          }`}
        >
          <span
            className={`absolute top-0.5 h-5 w-5 rounded-full bg-white shadow-sm transition-transform ${
              state.onlyAvailable ? "translate-x-5.5" : "translate-x-0.5"
            }`}
          />
        </button>
      </div>

      <Button variant="outline" onClick={onReset} className="w-full">
        {t("filters.resetFilters")}
      </Button>
    </div>
  );
}
