import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { BadgeCheck, Handshake, MapPinned, MessageSquareHeart } from "lucide-react";
import { SectionHeading } from "@/components/ui/SectionHeading";
import { useWhyChooseUs } from "@/data/site";

const ICONS = [Handshake, BadgeCheck, MapPinned, MessageSquareHeart];

export function WhyChooseUs() {
  const { t } = useTranslation("sections");
  const whyChooseUs = useWhyChooseUs();
  return (
    <section className="container-page py-14 sm:py-20">
      <SectionHeading
        eyebrow={t("whyChooseUs.eyebrow")}
        title={t("whyChooseUs.title")}
      />

      <div className="mt-14 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
        {whyChooseUs.map((item, i) => {
          const Icon = ICONS[i];
          return (
            <motion.div
              key={item.id}
              initial={{ opacity: 0, y: 24 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true, margin: "-40px" }}
              transition={{ duration: 0.5, delay: i * 0.08 }}
              whileHover={{ y: -6 }}
              className="group relative flex flex-col gap-4 overflow-hidden rounded-2xl border border-stone-100 bg-white p-7 transition-shadow hover:shadow-(--shadow-card) dark:border-stone-800 dark:bg-stone-900"
            >
              <div className="absolute -right-6 -top-6 h-24 w-24 rounded-full bg-grove-50 transition-transform duration-500 group-hover:scale-150 dark:bg-grove-950" />
              <span className="relative flex h-12 w-12 items-center justify-center rounded-xl bg-grove-700 text-white shadow-(--shadow-glow-grove)">
                <Icon size={21} />
              </span>
              <h3 className="relative font-display text-lg text-stone-900 dark:text-stone-50">{item.title}</h3>
              <p className="relative text-sm leading-relaxed text-stone-500 dark:text-stone-400">{item.description}</p>
            </motion.div>
          );
        })}
      </div>
    </section>
  );
}
