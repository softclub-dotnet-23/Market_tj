import { useState } from "react";
import { motion } from "framer-motion";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { Mail, Send } from "lucide-react";
import { PhotoTile } from "@/components/ui/PhotoTile";
import { productPhotos } from "@/assets/photos";

const floaterIds = [201, 203, 501];

export function NewsletterSection() {
  const { t } = useTranslation(["sections", "layout", "common"]);
  const [email, setEmail] = useState("");

  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim()) return;
    toast.success(t("sections:newsletter.toastTitle"), { description: t("sections:newsletter.toastDescription") });
    setEmail("");
  };

  return (
    <section className="container-page pb-24 pt-4">
      <motion.div
        initial={{ opacity: 0, y: 24 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.6 }}
        className="relative overflow-hidden rounded-[2rem] border border-harvest-200 bg-linear-to-br from-harvest-50 via-white to-grove-50 px-8 py-14 sm:px-14 dark:border-stone-800 dark:from-stone-900 dark:via-stone-900 dark:to-grove-950"
      >
        <div className="absolute -right-6 top-6 hidden gap-4 sm:flex">
          {floaterIds.map((id, i) => (
            <div
              key={id}
              className="h-16 w-16 overflow-hidden rounded-2xl shadow-(--shadow-card)"
              style={{ animation: `float 5s ease-in-out ${i * 0.6}s infinite` }}
            >
              <PhotoTile src={productPhotos[id]} alt="" className="h-full w-full" />
            </div>
          ))}
        </div>

        <div className="relative flex max-w-lg flex-col gap-4">
          <span className="inline-flex w-fit items-center gap-2 rounded-full bg-white px-3.5 py-1.5 text-xs font-semibold text-harvest-700 shadow-sm dark:bg-stone-800 dark:text-harvest-300">
            <Mail size={13} />
            {t("sections:newsletter.badge")}
          </span>
          <h2 className="text-balance font-display text-3xl leading-tight text-stone-900 dark:text-stone-50">
            {t("sections:newsletter.title")}
          </h2>
          <p className="text-balance text-stone-500 dark:text-stone-400">
            {t("sections:newsletter.description")}
          </p>
          <form onSubmit={submit} className="mt-2 flex flex-col gap-3 sm:flex-row">
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder={t("layout:footer.emailPlaceholder")}
              className="h-13 w-full rounded-xl border border-stone-200 bg-white px-5 text-[15px] focus:border-grove-500 focus:ring-2 focus:ring-grove-100 sm:max-w-xs dark:border-stone-700 dark:bg-stone-800 dark:text-stone-100 dark:placeholder:text-stone-500 dark:focus:ring-grove-900"
            />
            <button
              type="submit"
              className="flex h-13 items-center justify-center gap-2 rounded-xl bg-stone-900 px-6 text-sm font-semibold text-white transition hover:bg-stone-800 dark:bg-grove-600 dark:hover:bg-grove-500"
            >
              {t("common:actions.subscribe")}
              <Send size={15} />
            </button>
          </form>
        </div>
      </motion.div>
    </section>
  );
}
