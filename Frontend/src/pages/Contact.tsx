import { useState } from "react";
import { motion } from "framer-motion";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { Clock, Mail, MapPin, MessageCircle, Phone, Send } from "lucide-react";
import { Breadcrumbs } from "@/components/ui/Breadcrumbs";
import { Input, Textarea } from "@/components/ui/Field";
import { Dropdown } from "@/components/ui/Dropdown";
import { Button } from "@/components/ui/Button";
import { FAQSection } from "@/components/sections/FAQSection";
import { FacebookIcon, InstagramIcon, TelegramIcon, WhatsAppIcon } from "@/components/ui/SocialIcons";
import { officeInfo } from "@/data/site";

export function Contact() {
  const { t } = useTranslation(["pages", "layout"]);
  const SUBJECTS = [
    { value: "general", label: t("pages:contact.subjectGeneral") },
    { value: "farmer", label: t("pages:contact.subjectFarmer") },
    { value: "order", label: t("pages:contact.subjectOrder") },
    { value: "partnership", label: t("pages:contact.subjectPartnership") },
  ];

  const CONTACT_CARDS = [
    { icon: MapPin, label: t("pages:contact.cardAddress"), value: officeInfo.address },
    { icon: Phone, label: t("pages:contact.cardPhone"), value: officeInfo.phone },
    { icon: Mail, label: t("pages:contact.cardEmail"), value: officeInfo.email },
    { icon: Clock, label: t("pages:contact.cardHours"), value: officeInfo.hours },
  ];

  const [subject, setSubject] = useState(SUBJECTS[0].value);
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setTimeout(() => {
      setSubmitting(false);
      toast.success(t("pages:contact.toastTitle"), { description: t("pages:contact.toastDescription") });
      (e.target as HTMLFormElement).reset();
    }, 700);
  };

  return (
    <div>
      <div className="container-page pb-4 pt-8">
        <Breadcrumbs items={[{ label: t("layout:nav.contact") }]} />
      </div>

      <section className="container-page pb-16 pt-8 text-center">
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="mx-auto flex max-w-2xl flex-col items-center gap-4"
        >
          <span className="inline-flex items-center gap-2 rounded-full border border-grove-200 bg-grove-50 px-3.5 py-1.5 text-xs font-semibold uppercase tracking-[0.14em] text-grove-700 dark:border-grove-800 dark:bg-grove-950 dark:text-grove-300">
            <MessageCircle size={13} />
            {t("pages:contact.badge")}
          </span>
          <h1 className="text-balance font-display text-4xl text-stone-900 sm:text-5xl dark:text-stone-50">
            {t("pages:contact.title")}
          </h1>
          <p className="text-balance text-lg text-stone-500 dark:text-stone-400">
            {t("pages:contact.description")}
          </p>
        </motion.div>
      </section>

      <section className="container-page pb-24">
        <div className="grid grid-cols-1 gap-8 lg:grid-cols-[1fr_0.85fr]">
          <motion.form
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.5 }}
            onSubmit={onSubmit}
            className="flex flex-col gap-5 rounded-3xl border border-stone-100 bg-white p-6 sm:p-8 dark:border-stone-800 dark:bg-stone-900"
          >
            <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("pages:contact.formTitle")}</h2>
            <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
              <Input label={t("pages:contact.nameLabel")} placeholder={t("pages:contact.namePlaceholder")} required />
              <Input label={t("pages:contact.phoneLabel")} type="tel" placeholder="+992 __ ___ ____" required />
            </div>
            <Input label={t("pages:contact.cardEmail")} type="email" placeholder="you@example.com" required />
            <div className="flex flex-col gap-1.5">
              <span className="text-sm font-medium text-stone-700 dark:text-stone-300">{t("pages:contact.subjectLabel")}</span>
              <Dropdown options={SUBJECTS} value={subject} onChange={setSubject} />
            </div>
            <Textarea label={t("pages:contact.messageLabel")} placeholder={t("pages:contact.messagePlaceholder")} required rows={5} />
            <Button type="submit" size="lg" loading={submitting} rightIcon={<Send size={16} />}>
              {t("pages:contact.submit")}
            </Button>
          </motion.form>

          <div className="flex flex-col gap-6">
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.5, delay: 0.1 }}
              className="grid grid-cols-1 gap-3 sm:grid-cols-2"
            >
              {CONTACT_CARDS.map((card) => (
                <div key={card.label} className="flex flex-col gap-2 rounded-2xl border border-stone-100 bg-white p-5 dark:border-stone-800 dark:bg-stone-900">
                  <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-grove-50 text-grove-700 dark:bg-grove-950 dark:text-grove-400">
                    <card.icon size={17} />
                  </span>
                  <p className="text-xs text-stone-400 dark:text-stone-500">{card.label}</p>
                  <p className="text-sm font-medium text-stone-800 dark:text-stone-100">{card.value}</p>
                </div>
              ))}
            </motion.div>

            <motion.div
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.5, delay: 0.2 }}
              className="relative flex-1 overflow-hidden rounded-3xl border border-stone-100 bg-linear-to-br from-grove-50 via-stone-50 to-harvest-50 dark:border-stone-800 dark:from-grove-950 dark:via-stone-900 dark:to-stone-900"
            >
              <div className="absolute inset-0 opacity-40" style={{
                backgroundImage:
                  "linear-gradient(rgba(60,54,45,0.08) 1px, transparent 1px), linear-gradient(90deg, rgba(60,54,45,0.08) 1px, transparent 1px)",
                backgroundSize: "28px 28px",
              }} />
              <div className="relative flex h-full min-h-52 flex-col items-center justify-center gap-2 p-8 text-center">
                <span className="flex h-12 w-12 items-center justify-center rounded-full bg-grove-700 text-white shadow-(--shadow-glow-grove)">
                  <MapPin size={20} />
                </span>
                <p className="text-sm font-semibold text-stone-800 dark:text-stone-100">{officeInfo.address}</p>
                <p className="text-xs text-stone-400 dark:text-stone-500">{t("pages:contact.mapComingSoon")}</p>
              </div>
            </motion.div>

            <motion.div
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.5, delay: 0.3 }}
              className="flex items-center justify-between rounded-2xl border border-stone-100 bg-white p-5 dark:border-stone-800 dark:bg-stone-900"
            >
              <span className="text-sm font-medium text-stone-700 dark:text-stone-300">{t("pages:contact.socialTitle")}</span>
              <div className="flex items-center gap-2">
                {[InstagramIcon, TelegramIcon, FacebookIcon, WhatsAppIcon].map((Icon, i) => (
                  <a
                    key={i}
                    href="#"
                    onClick={(e) => e.preventDefault()}
                    className="flex h-9 w-9 items-center justify-center rounded-full bg-stone-100 text-stone-500 transition hover:bg-grove-100 hover:text-grove-700 dark:bg-stone-800 dark:text-stone-400 dark:hover:bg-grove-900 dark:hover:text-grove-300"
                  >
                    <Icon className="h-4 w-4" />
                  </a>
                ))}
              </div>
            </motion.div>
          </div>
        </div>
      </section>

      <FAQSection />
    </div>
  );
}
