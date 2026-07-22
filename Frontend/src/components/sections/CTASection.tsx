import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { ArrowRight, Sprout, UserRound } from "lucide-react";
import { PhotoTile } from "@/components/ui/PhotoTile";
import { ctaOrchardPhoto } from "@/assets/photos";

export function CTASection() {
  const { t } = useTranslation("sections");
  return (
    <section className="relative isolate -mb-24 overflow-hidden text-white">
      <PhotoTile src={ctaOrchardPhoto} alt="" className="absolute inset-0" imgClassName="object-[50%_65%]" />
      <div className="absolute inset-0 bg-linear-to-r from-grove-950 via-grove-950/85 to-grove-950/25" />
      <div className="pointer-events-none absolute inset-0 bg-noise opacity-15" />
      <div className="absolute inset-x-0 bottom-0 h-28 bg-linear-to-b from-transparent to-stone-950" />

      <div className="container-page relative py-24 sm:py-32">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.6 }}
          className="flex max-w-xl flex-col gap-5"
        >
          <h2 className="text-balance font-display text-3xl leading-tight text-white sm:text-4xl">{t("cta.title")}</h2>
          <p className="max-w-md text-balance text-grove-100/90">{t("cta.description")}</p>
          <div className="mt-2 flex flex-wrap items-center gap-3">
            <Link
              to="/register"
              className="group inline-flex items-center justify-center gap-2 rounded-xl bg-white px-6 py-3.5 text-sm font-semibold text-grove-800 transition hover:bg-grove-50"
            >
              <UserRound size={16} />
              {t("cta.becomeCustomer")}
              <ArrowRight size={15} className="transition-transform group-hover:translate-x-1" />
            </Link>
            <Link
              to="/register"
              className="group inline-flex items-center justify-center gap-2 rounded-xl border border-white/30 px-6 py-3.5 text-sm font-semibold text-white transition hover:bg-white/10"
            >
              <Sprout size={16} />
              {t("cta.becomeFarmer")}
              <ArrowRight size={15} className="transition-transform group-hover:translate-x-1" />
            </Link>
          </div>
        </motion.div>
      </div>
    </section>
  );
}
