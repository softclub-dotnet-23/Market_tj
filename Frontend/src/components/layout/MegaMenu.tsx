import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { ArrowRight } from "lucide-react";
import { useCategories } from "@/data/categories";
import { PhotoTile } from "@/components/ui/PhotoTile";
import { categoryPhotos } from "@/assets/photos";

export function MegaMenu({ onNavigate }: { onNavigate?: () => void }) {
  const { t } = useTranslation(["layout", "product"]);
  const categories = useCategories();
  return (
    <motion.div
      initial={{ opacity: 0, y: -8 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -8 }}
      transition={{ duration: 0.2, ease: [0.25, 1, 0.5, 1] }}
      className="absolute left-1/2 top-full z-40 mt-3 w-[min(880px,92vw)] -translate-x-1/2 rounded-3xl border border-stone-100 bg-white p-6 shadow-(--shadow-lifted) dark:border-stone-800 dark:bg-stone-900"
    >
      <div className="grid grid-cols-3 gap-3">
        {categories.map((category) => {
          const Icon = category.icon;
          return (
            <Link
              key={category.id}
              to={`/catalog?category=${category.slug}`}
              onClick={onNavigate}
              className="group flex items-center gap-3 rounded-2xl p-3 transition hover:bg-stone-50 dark:hover:bg-stone-800"
            >
              <div className="h-14 w-14 shrink-0 overflow-hidden rounded-xl">
                <PhotoTile src={categoryPhotos[category.photoKey]} alt={category.name} className="h-full w-full" />
              </div>
              <div className="min-w-0">
                <p className="flex items-center gap-1 font-display text-[15px] text-stone-900 dark:text-stone-100">
                  <Icon size={14} className="text-grove-600 dark:text-grove-400" />
                  {category.name}
                </p>
                <p className="truncate text-xs text-stone-400 dark:text-stone-500">
                  {t("product:productsCount", { count: category.productCount })}
                </p>
              </div>
            </Link>
          );
        })}
      </div>
      <Link
        to="/catalog"
        onClick={onNavigate}
        className="mt-4 flex items-center justify-between rounded-2xl bg-grove-50 px-5 py-4 text-sm font-semibold text-grove-800 transition hover:bg-grove-100 dark:bg-grove-950 dark:text-grove-200 dark:hover:bg-grove-900"
      >
        {t("layout:megaMenu.viewAllCatalog")}
        <ArrowRight size={16} />
      </Link>
    </motion.div>
  );
}
