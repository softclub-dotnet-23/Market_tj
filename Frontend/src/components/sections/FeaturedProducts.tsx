import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { ArrowUpRight } from "lucide-react";
import { SectionHeading } from "@/components/ui/SectionHeading";
import { ProductCard } from "@/components/product/ProductCard";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { products } from "@/data/products";

const bestsellers = products.filter((p) => p.badges.includes("bestseller")).slice(0, 8);
const newArrivals = products.filter((p) => p.badges.includes("new")).slice(0, 8);
const premium = products.filter((p) => p.badges.includes("premium")).slice(0, 8);

function Grid({ items }: { items: typeof products }) {
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
