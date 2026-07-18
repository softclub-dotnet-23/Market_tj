import { useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { ArrowRight, BadgeCheck, PlayCircle, Sparkles } from "lucide-react";
import { PhotoTile } from "@/components/ui/PhotoTile";
import { Avatar } from "@/components/ui/Avatar";
import { products } from "@/data/products";
import { farmers } from "@/data/farmers";
import { productPhotos, heroPhoto, farmerPhotos } from "@/assets/photos";

const floatingIds = [201, 401, 501, 601];
const avatarFarmerIds = [1, 2, 3, 8];

export function Hero() {
  const { t } = useTranslation(["sections", "layout", "common"]);
  const navigate = useNavigate();
  const floaters = floatingIds.map((id) => products.find((p) => p.id === id)!).filter(Boolean);

  return (
    <section className="relative overflow-hidden pb-10 pt-14 sm:pb-14 sm:pt-20">
      <div className="pointer-events-none absolute inset-0 -z-10">
        <div className="absolute -top-40 left-1/2 h-[560px] w-[900px] -translate-x-1/2 rounded-full bg-linear-to-br from-grove-100 via-harvest-50 to-transparent opacity-70 blur-3xl" />
        <div className="absolute -right-20 top-40 h-72 w-72 rounded-full bg-clay-100 opacity-40 blur-3xl" />
      </div>

      <div className="container-page grid grid-cols-1 items-center gap-16 lg:grid-cols-2">
        <div className="flex flex-col items-start gap-7">
          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5 }}
            className="inline-flex items-center gap-2 rounded-full border border-grove-200 bg-white/80 px-4 py-1.5 text-xs font-semibold text-grove-700 shadow-sm backdrop-blur dark:border-grove-800 dark:bg-grove-950/60 dark:text-grove-300"
          >
            <Sparkles size={13} />
            {t("sections:hero.badge")}
          </motion.div>

          <motion.h1
            initial={{ opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.08, ease: [0.25, 1, 0.5, 1] }}
            className="text-balance text-[2.6rem] leading-[1.06] text-stone-900 sm:text-6xl dark:text-stone-50"
          >
            {t("sections:hero.titleLine1")}
            <br />
            <span className="text-grove-700 dark:text-grove-400">{t("sections:hero.titleLine2")}</span>
          </motion.h1>

          <motion.p
            initial={{ opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.16 }}
            className="max-w-md text-balance text-lg leading-relaxed text-stone-500 dark:text-stone-400"
          >
            {t("sections:hero.description")}
          </motion.p>

          <motion.div
            initial={{ opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.24 }}
            className="flex flex-wrap items-center gap-3"
          >
            <button
              onClick={() => navigate("/catalog")}
              className="group inline-flex h-13 items-center gap-2 rounded-xl bg-grove-700 px-7 text-base font-medium text-white shadow-(--shadow-glow-grove) transition hover:bg-grove-800"
            >
              {t("sections:hero.viewCatalog")}
              <ArrowRight size={18} className="transition-transform group-hover:translate-x-1" />
            </button>
            <button className="inline-flex h-13 items-center gap-2.5 rounded-xl px-5 text-base font-medium text-stone-700 transition hover:text-grove-700 dark:text-stone-300 dark:hover:text-grove-400">
              <span className="flex h-9 w-9 items-center justify-center rounded-full bg-white shadow-(--shadow-soft) dark:bg-stone-800">
                <PlayCircle size={18} />
              </span>
              {t("sections:hero.howItWorks")}
            </button>
          </motion.div>

          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ duration: 0.7, delay: 0.4 }}
            className="flex items-center gap-3 pt-2"
          >
            <div className="flex -space-x-3">
              {avatarFarmerIds.map((id) => {
                const farmer = farmers.find((f) => f.id === id)!;
                return <Avatar key={id} name={farmer.ownerName} src={farmerPhotos[id]} size={36} ring />;
              })}
            </div>
            <p className="text-sm text-stone-500 dark:text-stone-400">
              <span className="font-semibold text-stone-800 dark:text-stone-200">32 400+</span>{" "}
              {t("sections:hero.ordersDeliveredSuffix")}
            </p>
          </motion.div>
        </div>

        <div className="relative mx-auto hidden aspect-square w-full max-w-md lg:block">
          <motion.div
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.8, ease: [0.25, 1, 0.5, 1] }}
            className="absolute inset-8 overflow-hidden rounded-[2.5rem] shadow-(--shadow-lifted)"
          >
            <PhotoTile src={heroPhoto} alt={t("layout:authPanel.heroAlt")} className="h-full w-full" />
          </motion.div>

          {floaters.map((product, i) => {
            const positions = [
              "left-0 top-4",
              "right-0 top-24",
              "left-2 bottom-8",
              "right-4 bottom-0",
            ];
            return (
              <motion.div
                key={product.id}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.6, delay: 0.5 + i * 0.12 }}
                className={`absolute z-10 w-28 rounded-2xl border border-white bg-white p-2.5 shadow-(--shadow-lifted) ${positions[i]}`}
                style={{ animation: `float 6s ease-in-out ${i * 0.7}s infinite` }}
              >
                <div className="mb-2 aspect-square overflow-hidden rounded-xl">
                  <PhotoTile src={productPhotos[product.id]} alt={product.title} className="h-full w-full" />
                </div>
                <p className="truncate text-[11px] font-medium text-stone-700">{product.title}</p>
                <p className="text-xs font-semibold text-grove-700">
                  {product.retailPricePerKg} {t("common:currencySomoni")}
                </p>
              </motion.div>
            );
          })}

          <motion.div
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.6, delay: 1 }}
            className="absolute left-1/2 top-1/2 z-20 flex -translate-x-1/2 -translate-y-1/2 items-center gap-2 rounded-2xl bg-white px-4 py-3 shadow-(--shadow-lifted)"
          >
            <BadgeCheck size={20} className="text-grove-600" />
            <div>
              <p className="text-xs font-semibold text-stone-800">{t("sections:hero.farmerVerifiedTitle")}</p>
              <p className="text-[11px] text-stone-400">{t("sections:hero.farmerVerifiedSubtitle")}</p>
            </div>
          </motion.div>
        </div>
      </div>
    </section>
  );
}
