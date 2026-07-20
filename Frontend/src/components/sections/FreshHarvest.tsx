import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { ArrowRight, Clock, Leaf } from "lucide-react";
import { PhotoTile } from "@/components/ui/PhotoTile";
import { RatingStars } from "@/components/ui/RatingStars";
import { useProducts } from "@/data/products";
import { productPhotos } from "@/assets/photos";
import { formatSomoni, timeAgo } from "@/lib/utils";

export function FreshHarvest() {
  const { t } = useTranslation(["sections", "common", "product"]);
  const products = useProducts();
  const freshest = [...products]
    .sort((a, b) => new Date(b.harvestDate).getTime() - new Date(a.harvestDate).getTime())
    .slice(0, 5);
  const [big, ...rest] = freshest;

  return (
    <section className="py-14 sm:py-20">
      <div className="container-page grid grid-cols-1 gap-12 lg:grid-cols-[0.85fr_1.15fr] lg:items-center">
        <div className="flex flex-col items-start gap-6">
          <span className="inline-flex items-center gap-2 rounded-full border border-grove-200 bg-grove-50 px-3.5 py-1.5 text-xs font-semibold uppercase tracking-[0.14em] text-grove-700 dark:border-grove-800 dark:bg-grove-950 dark:text-grove-300">
            <Leaf size={13} />
            {t("sections:freshHarvest.badge")}
          </span>
          <h2 className="text-balance font-display text-3xl leading-[1.12] text-stone-900 sm:text-[2.6rem] dark:text-stone-50">
            {t("sections:freshHarvest.title")}
          </h2>
          <p className="text-balance text-[15px] leading-relaxed text-stone-500 sm:text-base dark:text-stone-400">
            {t("sections:freshHarvest.description")}
          </p>
          <Link
            to="/catalog?sortBy=fresh"
            className="inline-flex items-center gap-2 rounded-xl bg-stone-900 px-6 py-3.5 text-sm font-medium text-white transition hover:bg-stone-800 dark:bg-grove-600 dark:hover:bg-grove-500"
          >
            {t("sections:freshHarvest.viewFresh")}
            <ArrowRight size={16} />
          </Link>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.5 }}
            className="col-span-2 flex overflow-hidden rounded-2xl border border-stone-100 bg-white sm:h-56 dark:border-stone-800 dark:bg-stone-900"
          >
            <div className="relative w-2/5 shrink-0">
              <PhotoTile src={productPhotos[big.id]} alt={big.title} className="h-full w-full" />
            </div>
            <div className="flex flex-1 flex-col justify-center gap-2 p-5">
              <span className="inline-flex w-fit items-center gap-1 rounded-full bg-grove-100 px-2.5 py-1 text-[11px] font-semibold text-grove-800 dark:bg-grove-900 dark:text-grove-200">
                <Clock size={11} />
                {timeAgo(big.harvestDate)}
              </span>
              <Link to={`/product/${big.slug}`}>
                <h3 className="font-display text-xl text-stone-900 hover:text-grove-700 dark:text-stone-50 dark:hover:text-grove-400">{big.title}</h3>
              </Link>
              <RatingStars rating={big.rating} size={13} showValue reviewCount={big.reviewCount} />
              <p className="font-display text-lg text-stone-900 dark:text-stone-50">
                {formatSomoni(big.retailPricePerKg)} {t("common:currencySomoni")}/{t(`product:units.${big.unit}`)}
              </p>
            </div>
          </motion.div>

          {rest.map((product, i) => {
            return (
              <motion.div
                key={product.id}
                initial={{ opacity: 0, y: 20 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true }}
                transition={{ duration: 0.5, delay: 0.1 + i * 0.08 }}
              >
                <Link
                  to={`/product/${product.slug}`}
                  className="flex items-center gap-3 rounded-2xl border border-stone-100 bg-white p-3 transition hover:shadow-(--shadow-card) dark:border-stone-800 dark:bg-stone-900"
                >
                  <div className="h-16 w-16 shrink-0 overflow-hidden rounded-xl">
                    <PhotoTile src={productPhotos[product.id]} alt={product.title} className="h-full w-full" />
                  </div>
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{product.title}</p>
                    <span className="text-[11px] text-stone-400 dark:text-stone-500">{timeAgo(product.harvestDate)}</span>
                  </div>
                </Link>
              </motion.div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
