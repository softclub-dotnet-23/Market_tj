import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { ArrowLeft, Compass, Search } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { PhotoTile } from "@/components/ui/PhotoTile";
import { categoryPhotos } from "@/assets/photos";

export function NotFound() {
  const { t } = useTranslation("pages");
  return (
    <div className="container-page flex min-h-[calc(100vh-4.5rem)] flex-col items-center justify-center gap-8 py-16 text-center">
      <motion.div
        initial={{ opacity: 0, scale: 0.85, rotate: -6 }}
        animate={{ opacity: 1, scale: 1, rotate: 0 }}
        transition={{ duration: 0.6, ease: [0.25, 1, 0.5, 1] }}
        className="relative h-40 w-40"
      >
        <div className="h-full w-full overflow-hidden rounded-[2rem] shadow-(--shadow-lifted)">
          <PhotoTile src={categoryPhotos.dried} alt="" className="h-full w-full" />
        </div>
        <motion.span
          animate={{ y: [0, -8, 0] }}
          transition={{ duration: 2.4, repeat: Infinity, ease: "easeInOut" }}
          className="absolute -right-3 -top-3 flex h-11 w-11 items-center justify-center rounded-full bg-white text-stone-400 shadow-(--shadow-card) dark:bg-stone-800 dark:text-stone-400"
        >
          <Search size={18} />
        </motion.span>
      </motion.div>

      <div className="flex flex-col items-center gap-3">
        <span className="font-display text-7xl text-stone-200 dark:text-stone-700">404</span>
        <h1 className="font-display text-2xl text-stone-900 sm:text-3xl dark:text-stone-50">{t("notFound.title")}</h1>
        <p className="max-w-md text-balance text-stone-500 dark:text-stone-400">
          {t("notFound.description")}
        </p>
      </div>

      <div className="flex flex-wrap items-center justify-center gap-3">
        <Link to="/">
          <Button leftIcon={<ArrowLeft size={16} />}>{t("goHome")}</Button>
        </Link>
        <Link to="/catalog">
          <Button variant="outline" leftIcon={<Compass size={16} />}>
            {t("notFound.goToCatalog")}
          </Button>
        </Link>
      </div>
    </div>
  );
}
