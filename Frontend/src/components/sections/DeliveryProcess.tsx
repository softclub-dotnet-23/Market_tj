import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { Bike, ClipboardCheck, PackageCheck, ShoppingBasket } from "lucide-react";
import { SectionHeading } from "@/components/ui/SectionHeading";
import { deliverySteps } from "@/data/site";

const ICONS = [ShoppingBasket, ClipboardCheck, Bike, PackageCheck];

export function DeliveryProcess() {
  const { t } = useTranslation("sections");
  return (
    <section className="bg-stone-50/60 py-14 sm:py-20 dark:bg-stone-900/40">
      <div className="container-page">
        <SectionHeading
          eyebrow={t("deliveryProcess.eyebrow")}
          title={t("deliveryProcess.title")}
        />

        <div className="relative mt-16 grid grid-cols-1 gap-10 sm:grid-cols-2 lg:grid-cols-4">
          <div className="pointer-events-none absolute left-0 right-0 top-8 hidden h-px bg-linear-to-r from-transparent via-stone-200 to-transparent lg:block dark:via-stone-700" />
          {deliverySteps.map((step, i) => {
            const Icon = ICONS[i];
            return (
              <motion.div
                key={step.id}
                initial={{ opacity: 0, y: 24 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true, margin: "-60px" }}
                transition={{ duration: 0.5, delay: i * 0.1 }}
                className="relative flex flex-col items-start gap-4"
              >
                <div className="relative flex h-16 w-16 items-center justify-center rounded-2xl bg-white text-grove-700 shadow-(--shadow-card) dark:bg-stone-800 dark:text-grove-400">
                  <Icon size={26} />
                  <span className="absolute -right-2 -top-2 flex h-6 w-6 items-center justify-center rounded-full bg-harvest-500 text-xs font-bold text-stone-900">
                    {i + 1}
                  </span>
                </div>
                <h3 className="font-display text-lg text-stone-900 dark:text-stone-50">{step.title}</h3>
                <p className="text-sm leading-relaxed text-stone-500 dark:text-stone-400">{step.description}</p>
              </motion.div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
