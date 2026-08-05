import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { PawPrint, Snowflake, Truck } from "lucide-react";
import { SectionHeading } from "@/components/ui/SectionHeading";
import { DeliveryTruckIllustration } from "@/components/illustrations/DeliveryTruckIllustration";

interface DeliveryFeature {
  title: string;
  description: string;
}

const ICONS = [Snowflake, PawPrint, Truck];

// По прямому запросу пользователя (2026-08-02) — раздел про доставку на
// информационной странице "О нас": удобство (не искать такси, просто
// заказать), спецтранспорт для продуктов (сохраняет свежесть) и для живности,
// простая логистика без отдельной панели курьера (координирует Market.tj,
// статус заказа виден и фермеру, и покупателю — реальная функциональность,
// см. OrderStatus.CourierAssigned/InDelivery в CustomerOrders/FarmerOrders).
// Своих фотографий грузовиков в проекте нет — иллюстрируем иконками в едином
// стиле сайта (тот же приём, что и в остальных секциях About/Contact).
export function DeliveryFleet() {
  const { t } = useTranslation("pages");
  const features = t("about.deliveryFeatures", { returnObjects: true }) as DeliveryFeature[];

  return (
    <section className="container-page py-14 sm:py-20">
      <div className="grid grid-cols-1 items-center gap-14 lg:grid-cols-2">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-60px" }}
          transition={{ duration: 0.5 }}
          className="flex flex-col gap-5"
        >
          <SectionHeading eyebrow={t("about.deliveryEyebrow")} title={t("about.deliveryTitle")} align="left" />
          <p className="max-w-lg text-balance leading-relaxed text-stone-500 dark:text-stone-400">
            {t("about.deliveryIntro")}
          </p>
          <DeliveryTruckIllustration className="mx-auto mt-4 w-full max-w-md lg:mx-0" />
        </motion.div>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {features.map((feature, i) => {
            const Icon = ICONS[i];
            return (
              <motion.div
                key={feature.title}
                initial={{ opacity: 0, y: 20 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true, margin: "-40px" }}
                transition={{ duration: 0.5, delay: i * 0.1 }}
                className={`flex flex-col gap-3 rounded-2xl border border-stone-100 bg-white p-6 dark:border-stone-800 dark:bg-stone-900 ${
                  i === 2 ? "sm:col-span-2" : ""
                }`}
              >
                <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-grove-50 text-grove-700 dark:bg-grove-950 dark:text-grove-400">
                  <Icon size={19} />
                </span>
                <h3 className="font-display text-base text-stone-900 dark:text-stone-50">{feature.title}</h3>
                <p className="text-sm leading-relaxed text-stone-500 dark:text-stone-400">{feature.description}</p>
              </motion.div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
