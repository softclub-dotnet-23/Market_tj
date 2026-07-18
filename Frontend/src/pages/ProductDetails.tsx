import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import {
  CalendarDays,
  Heart,
  Minus,
  PackageCheck,
  Plus,
  Share2,
  ShieldCheck,
  Sprout,
  Truck,
} from "lucide-react";
import { Breadcrumbs } from "@/components/ui/Breadcrumbs";
import { Badge } from "@/components/ui/Badge";
import { RatingStars } from "@/components/ui/RatingStars";
import { Button } from "@/components/ui/Button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { SectionHeading } from "@/components/ui/SectionHeading";
import { EmptyState } from "@/components/ui/EmptyState";
import { ProductGallery } from "@/components/product/ProductGallery";
import { FarmerProfileCard } from "@/components/product/FarmerProfileCard";
import { ReviewsSection } from "@/components/product/ReviewsSection";
import { ProductCard } from "@/components/product/ProductCard";
import { getProductBySlug, getRelatedProducts, products } from "@/data/products";
import { useCategories } from "@/data/categories";
import { getFarmerById } from "@/data/farmers";
import { getReviewsForProduct } from "@/data/reviews";
import { productPhotos, farmerPhotos } from "@/assets/photos";
import { useCart } from "@/context/CartContext";
import { useFavorites } from "@/context/FavoritesContext";
import { cn, formatDate, formatSomoni } from "@/lib/utils";

const BADGE_VARIANTS: Record<string, "grove" | "harvest" | "clay" | "dark"> = {
  organic: "grove",
  new: "harvest",
  bestseller: "dark",
  discount: "clay",
  premium: "harvest",
};

export function ProductDetails() {
  const { t } = useTranslation(["pages", "product", "common", "layout"]);
  const { slug } = useParams();
  const product = getProductBySlug(slug ?? "");
  const { addItem } = useCart();
  const { isFavorite, toggleFavorite } = useFavorites();
  const [quantity, setQuantity] = useState(product?.minimumOrderQuantity ?? 1);
  const categories = useCategories();

  if (!product) {
    return (
      <div className="container-page py-16">
        <EmptyState
          icon={<PackageCheck size={26} />}
          title={t("pages:productDetails.notFoundTitle")}
          description={t("pages:productDetails.notFoundDescription")}
          action={
            <Link to="/catalog">
              <Button variant="outline">{t("pages:productDetails.backToCatalog")}</Button>
            </Link>
          }
        />
      </div>
    );
  }

  const category = categories.find((c) => c.id === product.categoryId);
  const farmer = getFarmerById(product.farmerId);
  const reviews = getReviewsForProduct(product.id);
  const related = getRelatedProducts(product, 4);
  const recommended = products
    .filter((p) => p.id !== product.id && p.categoryId !== product.categoryId && p.rating >= 4.6)
    .slice(0, 4);

  const favorite = isFavorite(product.id);
  const outOfStock = product.status === "outOfStock" || product.availableQuantity === 0;
  const discount = product.oldPrice
    ? Math.round(((product.oldPrice - product.retailPricePerKg) / product.oldPrice) * 100)
    : 0;

  const changeQty = (delta: number) => {
    setQuantity((q) => Math.max(product.minimumOrderQuantity, Math.min(product.availableQuantity, q + delta)));
  };

  return (
    <div className="container-page py-8 sm:py-12">
      <Breadcrumbs
        items={[
          { label: category?.name ?? t("layout:nav.catalog"), to: `/catalog?category=${category?.slug}` },
          { label: product.title },
        ]}
        className="mb-6"
      />

      <div className="grid grid-cols-1 gap-12 lg:grid-cols-2">
        <ProductGallery
          src={productPhotos[product.id]}
          title={product.title}
        />

        <div className="flex flex-col gap-5">
          <div className="flex flex-wrap items-center gap-2">
            {product.badges.map((b) => (
              <Badge key={b} variant={BADGE_VARIANTS[b]}>
                {t(`product:badges.${b}`)}
              </Badge>
            ))}
            <Badge variant="outline">{product.qualityGrade}</Badge>
          </div>

          <h1 className="text-balance font-display text-3xl leading-tight text-stone-900 sm:text-4xl dark:text-stone-50">
            {product.title}
          </h1>

          <div className="flex flex-wrap items-center gap-4">
            <RatingStars rating={product.rating} size={15} showValue reviewCount={product.reviewCount} />
            <span className="h-4 w-px bg-stone-200 dark:bg-stone-700" />
            <span className="text-sm text-stone-400 dark:text-stone-500">
              {t("pages:productDetails.viewsCount", { count: product.viewCount })}
            </span>
            <span className="h-4 w-px bg-stone-200 dark:bg-stone-700" />
            <span className="text-sm text-stone-400 dark:text-stone-500">
              {t("pages:productDetails.ordersCount", { count: product.orderCount })}
            </span>
          </div>

          {farmer && (
            <Link
              to={`/catalog?farmer=${farmer.id}`}
              className="flex w-fit items-center gap-2 rounded-full bg-stone-50 py-1.5 pl-1.5 pr-4 text-sm text-stone-600 transition hover:bg-stone-100 dark:bg-stone-800 dark:text-stone-300 dark:hover:bg-stone-700"
            >
              <img
                src={farmerPhotos[farmer.id]}
                alt={farmer.ownerName}
                className="h-7 w-7 shrink-0 rounded-full object-cover"
              />
              {t("pages:productDetails.farmerPrefix")}{" "}
              <span className="font-medium text-stone-800 dark:text-stone-100">{farmer.farmName}</span>
            </Link>
          )}

          <div className="flex items-end gap-3 rounded-2xl bg-stone-50 p-5 dark:bg-stone-800/60">
            <div className="flex flex-col">
              <span className="font-display text-4xl text-stone-900 dark:text-stone-50">
                {formatSomoni(product.retailPricePerKg)} {t("common:currencySomoni")}
                <span className="ml-1.5 text-base font-normal text-stone-400 dark:text-stone-500">
                  /{t(`product:units.${product.unit}`)}
                </span>
              </span>
              {product.oldPrice && (
                <span className="mt-1 flex items-center gap-2 text-sm">
                  <span className="text-stone-400 line-through dark:text-stone-500">
                    {formatSomoni(product.oldPrice)} {t("common:currencySomoni")}
                  </span>
                  <span className="font-semibold text-clay-600 dark:text-clay-400">−{discount}%</span>
                </span>
              )}
            </div>
            {product.wholesalePricePerKg && (
              <div className="ml-auto text-right text-xs text-stone-500 dark:text-stone-400">
                <p className="font-semibold text-stone-700 dark:text-stone-200">
                  {t("pages:productDetails.wholesalePrefix")} {formatSomoni(product.wholesalePricePerKg)}{" "}
                  {t("common:currencySomoni")}/{t(`product:units.${product.unit}`)}
                </p>
                <p>
                  {t("pages:productDetails.fromPrefix")} {product.wholesaleMinimumQuantity}{" "}
                  {t(`product:units.${product.unit}`)}
                </p>
              </div>
            )}
          </div>

          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            {[
              { icon: CalendarDays, label: t("pages:productDetails.harvestDate"), value: formatDate(product.harvestDate) },
              { icon: Sprout, label: t("pages:productDetails.region"), value: product.district },
              { icon: PackageCheck, label: t("pages:productDetails.inStock"), value: `${product.availableQuantity} ${t(`product:units.${product.unit}`)}` },
              { icon: Truck, label: t("pages:productDetails.minOrder"), value: `${product.minimumOrderQuantity} ${t(`product:units.${product.unit}`)}` },
            ].map((item) => (
              <div key={item.label} className="flex flex-col gap-1.5 rounded-xl border border-stone-100 p-3 dark:border-stone-800">
                <item.icon size={15} className="text-grove-600 dark:text-grove-400" />
                <span className="text-[11px] text-stone-400 dark:text-stone-500">{item.label}</span>
                <span className="text-xs font-semibold text-stone-800 dark:text-stone-100">{item.value}</span>
              </div>
            ))}
          </div>

          <p className="text-[15px] leading-relaxed text-stone-500 dark:text-stone-400">{product.shortDescription}</p>

          <div className="flex flex-col gap-3 pt-1 sm:flex-row sm:items-center">
            <div className="flex h-13 items-center gap-3 rounded-xl border border-stone-200 px-2 dark:border-stone-700">
              <button
                onClick={() => changeQty(-1)}
                disabled={quantity <= product.minimumOrderQuantity}
                className="flex h-9 w-9 items-center justify-center rounded-lg text-stone-600 transition hover:bg-stone-100 disabled:opacity-30 dark:text-stone-300 dark:hover:bg-stone-800"
              >
                <Minus size={15} />
              </button>
              <span className="min-w-10 text-center text-sm font-semibold text-stone-800 dark:text-stone-100">
                {quantity} {t(`product:units.${product.unit}`)}
              </span>
              <button
                onClick={() => changeQty(1)}
                disabled={quantity >= product.availableQuantity}
                className="flex h-9 w-9 items-center justify-center rounded-lg text-stone-600 transition hover:bg-stone-100 disabled:opacity-30 dark:text-stone-300 dark:hover:bg-stone-800"
              >
                <Plus size={15} />
              </button>
            </div>

            <Button
              size="lg"
              className="flex-1"
              disabled={outOfStock}
              onClick={() => addItem(product, quantity)}
            >
              {outOfStock
                ? t("product:outOfStock")
                : t("pages:productDetails.addToCartWithPrice", {
                    price: `${formatSomoni(product.retailPricePerKg * quantity)} ${t("common:currencySomoni")}`,
                  })}
            </Button>

            <div className="flex gap-2">
              <button
                onClick={() => toggleFavorite(product.id)}
                className={cn(
                  "flex h-13 w-13 shrink-0 items-center justify-center rounded-xl border border-stone-200 text-stone-500 transition hover:border-clay-300 hover:text-clay-500 dark:border-stone-700 dark:text-stone-400",
                  favorite && "border-clay-300 bg-clay-50 text-clay-500 dark:border-clay-500/40 dark:bg-clay-500/15 dark:text-clay-400",
                )}
                aria-label={t("pages:productDetails.addToFavorites")}
              >
                <Heart size={18} fill={favorite ? "currentColor" : "none"} />
              </button>
              <button
                className="flex h-13 w-13 shrink-0 items-center justify-center rounded-xl border border-stone-200 text-stone-500 transition hover:border-stone-300 dark:border-stone-700 dark:text-stone-400 dark:hover:border-stone-600"
                aria-label={t("pages:productDetails.share")}
              >
                <Share2 size={18} />
              </button>
            </div>
          </div>

          <div className="flex items-center gap-2 pt-1 text-xs text-stone-400 dark:text-stone-500">
            <ShieldCheck size={14} className="text-grove-600 dark:text-grove-400" />
            {t("pages:productDetails.paymentNote")}
          </div>
        </div>
      </div>

      <div className="mt-16">
        <Tabs defaultValue="description">
          <TabsList>
            <TabsTrigger value="description">{t("pages:productDetails.tabDescription")}</TabsTrigger>
            <TabsTrigger value="specs">{t("pages:productDetails.tabSpecs")}</TabsTrigger>
            <TabsTrigger value="reviews">{t("pages:productDetails.tabReviews", { count: reviews.length })}</TabsTrigger>
          </TabsList>

          <div className="mt-8 max-w-3xl">
            <TabsContent value="description">
              <p className="text-[15px] leading-relaxed text-stone-600 dark:text-stone-300">{product.description}</p>
            </TabsContent>
            <TabsContent value="specs">
              <dl className="divide-y divide-stone-100 overflow-hidden rounded-2xl border border-stone-100 dark:divide-stone-800 dark:border-stone-800">
                {product.specifications.map((spec) => (
                  <div key={spec.label} className="flex justify-between bg-white px-5 py-3.5 text-sm dark:bg-stone-900">
                    <dt className="text-stone-500 dark:text-stone-400">{spec.label}</dt>
                    <dd className="font-medium text-stone-800 dark:text-stone-100">{spec.value}</dd>
                  </div>
                ))}
                <div className="flex justify-between bg-white px-5 py-3.5 text-sm dark:bg-stone-900">
                  <dt className="text-stone-500 dark:text-stone-400">{t("pages:productDetails.originRegion")}</dt>
                  <dd className="font-medium text-stone-800 dark:text-stone-100">
                    {product.region}, {product.district}
                  </dd>
                </div>
              </dl>
            </TabsContent>
            <TabsContent value="reviews">
              <ReviewsSection reviews={reviews} rating={product.rating} count={product.reviewCount} />
            </TabsContent>
          </div>
        </Tabs>
      </div>

      {farmer && (
        <div className="mt-16">
          <h2 className="mb-5 font-display text-2xl text-stone-900 dark:text-stone-50">{t("pages:productDetails.aboutFarmer")}</h2>
          <FarmerProfileCard farmer={farmer} />
        </div>
      )}

      {related.length > 0 && (
        <section className="mt-20">
          <SectionHeading align="left" title={t("pages:productDetails.relatedProducts")} className="mb-8" />
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            {related.map((p, i) => (
              <motion.div
                key={p.id}
                initial={{ opacity: 0, y: 16 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true }}
                transition={{ duration: 0.4, delay: i * 0.05 }}
              >
                <ProductCard product={p} />
              </motion.div>
            ))}
          </div>
        </section>
      )}

      {recommended.length > 0 && (
        <section className="mt-16">
          <SectionHeading align="left" title={t("pages:productDetails.recommendedProducts")} className="mb-8" />
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            {recommended.map((p, i) => (
              <motion.div
                key={p.id}
                initial={{ opacity: 0, y: 16 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true }}
                transition={{ duration: 0.4, delay: i * 0.05 }}
              >
                <ProductCard product={p} />
              </motion.div>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
