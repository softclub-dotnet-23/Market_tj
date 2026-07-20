import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { Compass, HandHeart, ShieldCheck, Sprout, Target } from "lucide-react";
import { Breadcrumbs } from "@/components/ui/Breadcrumbs";
import { SectionHeading } from "@/components/ui/SectionHeading";
import { AnimatedCounter } from "@/components/ui/AnimatedCounter";
import { PhotoTile } from "@/components/ui/PhotoTile";
import { CTASection } from "@/components/sections/CTASection";
import { useAboutValues, useImpactStats, useTimeline, useTeamValues } from "@/data/about";
import { categoryPhotos } from "@/assets/photos";

const VALUE_ICONS = [ShieldCheck, HandHeart, Sprout, Target];
const MISSION_PHOTO_KEYS = ["vegetables", "fruits", "dried", "dairy"];

export function About() {
  const { t } = useTranslation(["pages", "layout"]);
  const values = useAboutValues();
  const impactStats = useImpactStats();
  const timeline = useTimeline();
  const teamValues = useTeamValues();
  return (
    <div>
      <div className="container-page pb-4 pt-8">
        <Breadcrumbs items={[{ label: t("layout:nav.about") }]} />
      </div>

      <section className="container-page grid grid-cols-1 items-center gap-14 py-12 lg:grid-cols-2">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6 }}
          className="flex flex-col gap-6"
        >
          <span className="inline-flex w-fit items-center gap-2 rounded-full border border-grove-200 bg-grove-50 px-3.5 py-1.5 text-xs font-semibold uppercase tracking-[0.14em] text-grove-700 dark:border-grove-800 dark:bg-grove-950 dark:text-grove-300">
            <Compass size={13} />
            {t("pages:about.badge")}
          </span>
          <h1 className="text-balance font-display text-4xl leading-[1.1] text-stone-900 sm:text-5xl dark:text-stone-50">
            {t("pages:about.title")}
          </h1>
          <p className="text-balance text-lg leading-relaxed text-stone-500 dark:text-stone-400">
            {t("pages:about.paragraph1")}
          </p>
          <p className="text-balance leading-relaxed text-stone-500 dark:text-stone-400">
            {t("pages:about.paragraph2")}
          </p>
        </motion.div>

        <motion.div
          initial={{ opacity: 0, scale: 0.94 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ duration: 0.7, delay: 0.1 }}
          className="grid grid-cols-2 gap-4"
        >
          {MISSION_PHOTO_KEYS.map((key, i) => (
            <div
              key={key}
              className={`aspect-square overflow-hidden rounded-3xl ${i === 1 ? "translate-y-6" : ""} ${i === 2 ? "-translate-y-6" : ""}`}
            >
              <PhotoTile src={categoryPhotos[key]} alt="" className="h-full w-full" />
            </div>
          ))}
        </motion.div>
      </section>

      <section className="relative overflow-hidden bg-grove-900 py-16 text-white">
        <div className="pointer-events-none absolute inset-0 bg-noise opacity-20" />
        <div className="container-page relative grid grid-cols-2 gap-8 lg:grid-cols-4">
          {impactStats.map((stat, i) => (
            <motion.div
              key={stat.id}
              initial={{ opacity: 0, y: 16 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.5, delay: i * 0.08 }}
              className="flex flex-col items-center gap-1.5 text-center lg:items-start lg:text-left"
            >
              <AnimatedCounter value={stat.value} suffix={stat.suffix} className="font-display text-4xl text-white" />
              <p className="text-sm text-grove-200">{stat.label}</p>
            </motion.div>
          ))}
        </div>
      </section>

      <section className="container-page py-14 sm:py-20">
        <SectionHeading eyebrow={t("pages:about.valuesEyebrow")} title={t("pages:about.valuesTitle")} />
        <div className="mt-14 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {values.map((value, i) => {
            const Icon = VALUE_ICONS[i];
            return (
              <motion.div
                key={value.title}
                initial={{ opacity: 0, y: 20 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true, margin: "-40px" }}
                transition={{ duration: 0.5, delay: i * 0.08 }}
                className="flex flex-col gap-4 rounded-2xl border border-stone-100 bg-white p-7 dark:border-stone-800 dark:bg-stone-900"
              >
                <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-harvest-100 text-harvest-700 dark:bg-harvest-900 dark:text-harvest-200">
                  <Icon size={21} />
                </span>
                <h3 className="font-display text-lg text-stone-900 dark:text-stone-50">{value.title}</h3>
                <p className="text-sm leading-relaxed text-stone-500 dark:text-stone-400">{value.description}</p>
              </motion.div>
            );
          })}
        </div>
      </section>

      <section className="bg-stone-50/60 py-14 sm:py-20 dark:bg-stone-900/40">
        <div className="container-page">
          <SectionHeading eyebrow={t("pages:about.journeyEyebrow")} title={t("pages:about.journeyTitle")} align="left" />
          <div className="relative mt-14 flex flex-col gap-10 border-l border-stone-200 pl-8 sm:pl-10 dark:border-stone-700">
            {timeline.map((item, i) => (
              <motion.div
                key={item.year}
                initial={{ opacity: 0, x: -16 }}
                whileInView={{ opacity: 1, x: 0 }}
                viewport={{ once: true, margin: "-40px" }}
                transition={{ duration: 0.5, delay: i * 0.1 }}
                className="relative"
              >
                <span className="absolute -left-[calc(2rem+7px)] top-1 h-3.5 w-3.5 rounded-full border-2 border-grove-600 bg-white sm:-left-[calc(2.5rem+7px)] dark:bg-stone-950" />
                <span className="font-display text-sm text-grove-700 dark:text-grove-400">{item.year}</span>
                <h3 className="mt-1 font-display text-xl text-stone-900 dark:text-stone-50">{item.title}</h3>
                <p className="mt-2 max-w-xl text-[15px] leading-relaxed text-stone-500 dark:text-stone-400">{item.description}</p>
              </motion.div>
            ))}
          </div>
        </div>
      </section>

      <section className="container-page py-14 sm:py-20">
        <SectionHeading eyebrow={t("pages:about.teamEyebrow")} title={t("pages:about.teamTitle")} align="left" />
        <div className="mt-12 grid grid-cols-1 gap-5 sm:grid-cols-3">
          {teamValues.map((member, i) => (
            <motion.div
              key={member.name}
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.5, delay: i * 0.08 }}
              className="rounded-2xl border border-stone-100 bg-white p-7 dark:border-stone-800 dark:bg-stone-900"
            >
              <h3 className="font-display text-lg text-stone-900 dark:text-stone-50">{member.name}</h3>
              <p className="mt-2 text-sm leading-relaxed text-stone-500 dark:text-stone-400">{member.focus}</p>
            </motion.div>
          ))}
        </div>
      </section>

      <CTASection />
    </div>
  );
}
