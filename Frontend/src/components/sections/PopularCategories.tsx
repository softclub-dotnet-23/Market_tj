import { motion } from "framer-motion";
import { SectionHeading } from "@/components/ui/SectionHeading";
import { CategoryCard } from "@/components/product/CategoryCard";
import { categories } from "@/data/categories";

export function PopularCategories() {
  return (
    <section className="container-page py-14 sm:py-20">
      <SectionHeading eyebrow="Категории" title="Выбирайте по категориям" />
      <div className="mt-12 grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
        {categories.map((category, i) => (
          <motion.div
            key={category.id}
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-40px" }}
            transition={{ duration: 0.5, delay: i * 0.06 }}
          >
            <CategoryCard category={category} />
          </motion.div>
        ))}
      </div>
    </section>
  );
}
