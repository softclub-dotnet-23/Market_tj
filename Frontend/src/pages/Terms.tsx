import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { FileText, Mail, Phone } from "lucide-react";
import { Breadcrumbs } from "@/components/ui/Breadcrumbs";
import { useOfficeInfo } from "@/data/site";

interface LegalSection {
  heading: string;
  paragraphs: string[];
}

export function Terms() {
  const { t } = useTranslation(["pages", "layout"]);
  const officeInfo = useOfficeInfo();
  const sections = t("pages:terms.sections", { returnObjects: true }) as LegalSection[];

  return (
    <div>
      <div className="container-page pb-4 pt-8">
        <Breadcrumbs items={[{ label: t("layout:footer.terms") }]} />
      </div>

      <section className="container-page pb-8 pt-8 text-center">
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="mx-auto flex max-w-2xl flex-col items-center gap-4"
        >
          <span className="flex h-14 w-14 items-center justify-center rounded-2xl bg-grove-50 text-grove-700 dark:bg-grove-950 dark:text-grove-400">
            <FileText size={24} />
          </span>
          <h1 className="text-balance font-display text-4xl text-stone-900 sm:text-5xl dark:text-stone-50">
            {t("pages:terms.title")}
          </h1>
          <p className="text-balance text-lg text-stone-500 dark:text-stone-400">{t("pages:terms.subtitle")}</p>
          <p className="text-xs text-stone-400 dark:text-stone-500">{t("pages:terms.lastUpdated")}</p>
        </motion.div>
      </section>

      <section className="container-page pb-24">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.5 }}
          className="mx-auto flex max-w-3xl flex-col gap-10 rounded-3xl border border-stone-100 bg-white p-6 sm:p-10 dark:border-stone-800 dark:bg-stone-900"
        >
          {sections.map((section) => (
            <div key={section.heading} className="flex flex-col gap-3">
              <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{section.heading}</h2>
              {section.paragraphs.map((p, i) => (
                <p key={i} className="text-sm leading-relaxed text-stone-600 dark:text-stone-400">
                  {p}
                </p>
              ))}
            </div>
          ))}

          <div className="flex flex-col gap-3 rounded-2xl border border-grove-100 bg-grove-50 p-6 dark:border-grove-900 dark:bg-grove-950/40">
            <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("pages:terms.contactsTitle")}</h2>
            <p className="text-sm text-stone-600 dark:text-stone-400">{t("pages:terms.contactsDescription")}</p>
            <div className="flex flex-col gap-2 text-sm text-stone-700 sm:flex-row sm:gap-6 dark:text-stone-300">
              <a href={`mailto:${officeInfo.email}`} className="flex items-center gap-2 hover:text-grove-700 dark:hover:text-grove-400">
                <Mail size={15} className="shrink-0 text-grove-600 dark:text-grove-500" />
                {officeInfo.email}
              </a>
              <a href={`tel:${officeInfo.phone}`} className="flex items-center gap-2 hover:text-grove-700 dark:hover:text-grove-400">
                <Phone size={15} className="shrink-0 text-grove-600 dark:text-grove-500" />
                {officeInfo.phone}
              </a>
            </div>
          </div>
        </motion.div>
      </section>
    </div>
  );
}
