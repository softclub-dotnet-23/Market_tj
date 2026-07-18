import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { Heart, MapPin, Plus } from "lucide-react";
import type { Product } from "@/types";
import { PhotoTile } from "@/components/ui/PhotoTile";
import { RatingStars } from "@/components/ui/RatingStars";
import { Badge } from "@/components/ui/Badge";
import { getFarmerById } from "@/data/farmers";
import { useCart } from "@/context/CartContext";
import { useFavorites } from "@/context/FavoritesContext";
import { productPhotos } from "@/assets/photos";
import { cn, formatSomoni } from "@/lib/utils";

const BADGE_VARIANTS: Record<string, "grove" | "harvest" | "clay" | "dark"> = {
  organic: "grove",
  new: "harvest",
  bestseller: "dark",
  discount: "clay",
  premium: "harvest",
};

export function ProductCard({ product, className }: { product: Product; className?: string }) {
  const { t } = useTranslation(["product", "common"]);
  const farmer = getFarmerById(product.farmerId);
  const { addItem } = useCart();
  const { isFavorite, toggleFavorite } = useFavorites();
  const favorite = isFavorite(product.id);
  const outOfStock = product.status === "outOfStock" || product.availableQuantity === 0;

  return (
    <motion.div
      whileHover={{ y: -6 }}
      transition={{ type: "spring", stiffness: 300, damping: 22 }}
      className={cn(
        "group relative flex flex-col overflow-hidden rounded-2xl border border-stone-100 bg-white transition-shadow duration-300 hover:shadow-(--shadow-card) dark:border-stone-800 dark:bg-stone-900",
        className,
      )}
    >
      <Link to={`/product/${product.slug}`} className="relative block aspect-[4/3.4] overflow-hidden">
        <motion.div whileHover={{ scale: 1.06 }} transition={{ duration: 0.5, ease: [0.25, 1, 0.5, 1] }} className="h-full w-full">
          <PhotoTile src={productPhotos[product.id]} alt={product.title} className="h-full w-full" />
        </motion.div>

        <div className="absolute left-3 top-3 flex flex-col gap-1.5">
          {product.badges.slice(0, 2).map((b) => (
            <Badge key={b} variant={BADGE_VARIANTS[b]} className="shadow-sm">
              {t(`product:badges.${b}`)}
            </Badge>
          ))}
        </div>

        {outOfStock && (
          <div className="absolute inset-0 flex items-center justify-center bg-stone-900/45 backdrop-blur-[2px]">
            <span className="rounded-full bg-white px-4 py-1.5 text-sm font-semibold text-stone-800 dark:bg-stone-800 dark:text-stone-100">
              {t("product:outOfStock")}
            </span>
          </div>
        )}
      </Link>

      <button
        onClick={() => toggleFavorite(product.id)}
        aria-label={favorite ? t("product:removeFromFavorites") : t("product:addToFavorites")}
        aria-pressed={favorite}
        className={cn(
          "absolute right-3 top-3 flex h-9 w-9 items-center justify-center rounded-full bg-white/90 text-stone-500 shadow-sm backdrop-blur transition hover:scale-110 hover:text-clay-500 dark:bg-stone-800/90 dark:text-stone-300",
          favorite && "text-clay-500 dark:text-clay-400",
        )}
      >
        <Heart size={17} fill={favorite ? "currentColor" : "none"} />
      </button>

      <div className="flex flex-1 flex-col gap-2.5 p-4">
        <div className="flex items-center justify-between gap-2 text-xs text-stone-400 dark:text-stone-500">
          <span className="inline-flex items-center gap-1">
            <MapPin size={12} />
            {product.district}
          </span>
          <RatingStars rating={product.rating} size={12} />
        </div>

        <Link to={`/product/${product.slug}`}>
          <h3 className="line-clamp-2 font-display text-[17px] leading-snug text-stone-900 transition-colors group-hover:text-grove-800 dark:text-stone-50 dark:group-hover:text-grove-400">
            {product.title}
          </h3>
        </Link>

        {farmer && (
          <Link
            to={`/catalog?farmer=${farmer.id}`}
            className="text-xs font-medium text-stone-500 transition hover:text-grove-700 dark:text-stone-400 dark:hover:text-grove-400"
          >
            {farmer.farmName}
          </Link>
        )}

        <div className="mt-auto flex items-end justify-between pt-2">
          <div className="flex flex-col">
            {product.oldPrice && (
              <span className="text-xs text-stone-400 line-through dark:text-stone-500">
                {formatSomoni(product.oldPrice)} {t("common:currencySomoni")}
              </span>
            )}
            <span className="font-display text-xl text-stone-900 dark:text-stone-50">
              {formatSomoni(product.retailPricePerKg)} {t("common:currencySomoni")}
              <span className="ml-1 text-xs font-normal text-stone-400 dark:text-stone-500">
                /{t(`product:units.${product.unit}`)}
              </span>
            </span>
          </div>

          <motion.button
            whileTap={{ scale: 0.9 }}
            disabled={outOfStock}
            onClick={() => addItem(product)}
            aria-label={t("product:addToCart")}
            className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-grove-700 text-white shadow-(--shadow-glow-grove) transition hover:bg-grove-800 disabled:cursor-not-allowed disabled:bg-stone-200 disabled:text-stone-400 disabled:shadow-none dark:disabled:bg-stone-700 dark:disabled:text-stone-500"
          >
            <Plus size={18} />
          </motion.button>
        </div>
      </div>
    </motion.div>
  );
}
