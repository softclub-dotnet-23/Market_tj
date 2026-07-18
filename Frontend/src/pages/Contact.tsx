import { useState } from "react";
import { motion } from "framer-motion";
import { toast } from "sonner";
import { Clock, Mail, MapPin, MessageCircle, Phone, Send } from "lucide-react";
import { Breadcrumbs } from "@/components/ui/Breadcrumbs";
import { Input, Textarea } from "@/components/ui/Field";
import { Dropdown } from "@/components/ui/Dropdown";
import { Button } from "@/components/ui/Button";
import { FAQSection } from "@/components/sections/FAQSection";
import { FacebookIcon, InstagramIcon, TelegramIcon, WhatsAppIcon } from "@/components/ui/SocialIcons";
import { officeInfo } from "@/data/site";

const SUBJECTS = [
  { value: "general", label: "Общий вопрос" },
  { value: "farmer", label: "Хочу стать фермером" },
  { value: "order", label: "Вопрос по заказу" },
  { value: "partnership", label: "Партнёрство" },
];

const CONTACT_CARDS = [
  { icon: MapPin, label: "Адрес офиса", value: officeInfo.address },
  { icon: Phone, label: "Телефон", value: officeInfo.phone },
  { icon: Mail, label: "Email", value: officeInfo.email },
  { icon: Clock, label: "Часы работы", value: officeInfo.hours },
];

export function Contact() {
  const [subject, setSubject] = useState(SUBJECTS[0].value);
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setTimeout(() => {
      setSubmitting(false);
      toast.success("Сообщение отправлено!", { description: "Мы ответим вам в течение рабочего дня." });
      (e.target as HTMLFormElement).reset();
    }, 700);
  };

  return (
    <div>
      <div className="container-page pb-4 pt-8">
        <Breadcrumbs items={[{ label: "Контакты" }]} />
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
            Мы на связи
          </span>
          <h1 className="text-balance font-display text-4xl text-stone-900 sm:text-5xl dark:text-stone-50">Свяжитесь с нами</h1>
          <p className="text-balance text-lg text-stone-500 dark:text-stone-400">
            Вопросы по заказу, сотрудничеству или предложения по улучшению платформы — пишите, мы всегда рады помочь.
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
            <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">Напишите нам</h2>
            <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
              <Input label="Имя" placeholder="Ваше имя" required />
              <Input label="Телефон" type="tel" placeholder="+992 __ ___ ____" required />
            </div>
            <Input label="Email" type="email" placeholder="you@example.com" required />
            <div className="flex flex-col gap-1.5">
              <span className="text-sm font-medium text-stone-700 dark:text-stone-300">Тема обращения</span>
              <Dropdown options={SUBJECTS} value={subject} onChange={setSubject} />
            </div>
            <Textarea label="Сообщение" placeholder="Расскажите подробнее, чем мы можем помочь..." required rows={5} />
            <Button type="submit" size="lg" loading={submitting} rightIcon={<Send size={16} />}>
              Отправить сообщение
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
                <p className="text-xs text-stone-400 dark:text-stone-500">Интерактивная карта появится совсем скоро</p>
              </div>
            </motion.div>

            <motion.div
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.5, delay: 0.3 }}
              className="flex items-center justify-between rounded-2xl border border-stone-100 bg-white p-5 dark:border-stone-800 dark:bg-stone-900"
            >
              <span className="text-sm font-medium text-stone-700 dark:text-stone-300">Мы в соцсетях</span>
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
