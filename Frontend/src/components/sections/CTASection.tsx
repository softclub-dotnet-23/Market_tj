import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { ArrowRight, Sprout, UserRound } from "lucide-react";

export function CTASection() {
  const { t } = useTranslation("sections");
  return (
    <section className="container-page py-14 sm:py-20">
      <motion.div
        initial={{ opacity: 0, y: 24 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.6 }}
        className="relative overflow-hidden rounded-[2rem] bg-linear-to-br from-grove-800 via-grove-700 to-grove-900 px-8 py-14 text-white sm:px-14 sm:py-16"
      >
        <div className="pointer-events-none absolute inset-0 bg-noise opacity-20" />
        <div className="pointer-events-none absolute -right-16 -top-16 h-64 w-64 rounded-full bg-white/10 blur-3xl" />
        <div className="pointer-events-none absolute -bottom-20 left-1/3 h-64 w-64 rounded-full bg-harvest-500/20 blur-3xl" />

        <div className="relative grid grid-cols-1 items-center gap-10 lg:grid-cols-[1.2fr_auto]">
          <div className="flex flex-col gap-4">
            <h2 className="text-balance font-display text-3xl leading-tight sm:text-4xl">
              {t("cta.title")}
            </h2>
            <p className="max-w-xl text-balance text-grove-100">
              {t("cta.description")}
            </p>
          </div>

          <div className="flex flex-col gap-3 sm:flex-row lg:flex-col">
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
        </div>
      </motion.div>
    </section>
  );
}
