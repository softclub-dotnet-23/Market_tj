import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { ArrowUpRight } from "lucide-react";
import { SectionHeading } from "@/components/ui/SectionHeading";
import { ProductCard } from "@/components/product/ProductCard";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { useProducts } from "@/data/products";
import type { Product } from "@/types";

function Grid({ items }: { items: Product[] }) {
  return (
    <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 sm:gap-5 lg:grid-cols-4">
      {items.map((product, i) => (
        <motion.div
          key={product.id}
          initial={{ opacity: 0, y: 16 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-40px" }}
          transition={{ duration: 0.4, delay: (i % 4) * 0.06 }}
        >
          <ProductCard product={product} />
        </motion.div>
      ))}
    </div>
  );
}

export function FeaturedProducts() {
  const { t } = useTranslation("sections");
  const products = useProducts();
  // orderCount видит только Admin-сессия (см. catalogStore.ts — гостю/покупателю/
  // фермеру /order-items либо недоступен, либо отдаёт только СВОИ заказы) —
  // для абсолютного большинства посетителей orderCount всегда 0, и строгий
  // фильтр по бейджу "bestseller" оставлял вкладку вечно пустой. Сортируем по
  // orderCount, а при его отсутствии — по рейтингу×отзывам, чтобы вкладка
  // всегда показывала 8 живых товаров, а не пустоту.
  const bestsellers = [...products]
    .sort((a, b) => b.orderCount - a.orderCount || b.rating * b.reviewCount - a.rating * a.reviewCount)
    .slice(0, 8);
  // "Новинки" — реально самые недавно добавленные, а не только те, что
  // попадают под бейдж "new" (<=14 дней, см. catalogStore.ts) — тот порог
  // мог оставить вкладку пустой/неполной сразу после сидирования. Сортируем
  // по createdAt, вкладка всегда полная, пока в каталоге есть хотя бы 8 товаров.
  const newArrivals = [...products].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()).slice(0, 8);
  const premium = products.filter((p) => p.badges.includes("premium")).slice(0, 8);
  return (
    <section className="bg-stone-50/60 py-14 sm:py-20 dark:bg-stone-900/40">
      <div className="container-page">
        <SectionHeading
          eyebrow={t("featuredProducts.eyebrow")}
          align="left"
          title={t("featuredProducts.title")}
          action={
            <Link
              to="/catalog"
              className="inline-flex items-center gap-1.5 text-sm font-semibold text-grove-700 transition hover:text-grove-800 dark:text-grove-400 dark:hover:text-grove-300"
            >
              {t("featuredProducts.viewAll")}
              <ArrowUpRight size={16} />
            </Link>
          }
        />

        <Tabs defaultValue="bestsellers" className="mt-10">
          <TabsList>
            <TabsTrigger value="bestsellers">{t("featuredProducts.tabBestsellers")}</TabsTrigger>
            <TabsTrigger value="new">{t("featuredProducts.tabNew")}</TabsTrigger>
            <TabsTrigger value="premium">{t("featuredProducts.tabPremium")}</TabsTrigger>
          </TabsList>
          <div className="mt-8">
            <TabsContent value="bestsellers">
              <Grid items={bestsellers} />
            </TabsContent>
            <TabsContent value="new">
              <Grid items={newArrivals} />
            </TabsContent>
            <TabsContent value="premium">
              <Grid items={premium} />
            </TabsContent>
          </div>
        </Tabs>
      </div>
    </section>
  );
}
