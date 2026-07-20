import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import type { Category } from "@/types";
import { PhotoTile } from "@/components/ui/PhotoTile";
import { categoryPhotos } from "@/assets/photos";
import { cn } from "@/lib/utils";

export function CategoryCard({ category, className }: { category: Category; className?: string }) {
  const { t } = useTranslation("product");
  return (
    <motion.div whileHover={{ y: -5 }} transition={{ type: "spring", stiffness: 300, damping: 22 }}>
      <Link
        to={`/catalog?category=${category.slug}`}
        className={cn(
          "group relative flex flex-col overflow-hidden rounded-2xl border border-stone-100 bg-white transition-shadow duration-300 hover:shadow-(--shadow-card) dark:border-stone-800 dark:bg-stone-900",
          className,
        )}
      >
        <div className="relative aspect-square overflow-hidden">
          <motion.div whileHover={{ scale: 1.08 }} transition={{ duration: 0.5 }} className="h-full w-full">
            <PhotoTile src={categoryPhotos[category.photoKey]} alt={category.name} className="h-full w-full" />
          </motion.div>
        </div>
        <div className="flex flex-col gap-0.5 p-4">
          <h3 className="font-display text-base text-stone-900 dark:text-stone-50">{category.name}</h3>
          <p className="text-xs text-stone-400 dark:text-stone-500">
            {t("productsCount", { count: category.productCount })}
          </p>
        </div>
      </Link>
    </motion.div>
  );
}
