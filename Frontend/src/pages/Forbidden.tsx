import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { ArrowLeft, LifeBuoy, ShieldAlert } from "lucide-react";
import { Button } from "@/components/ui/Button";

export function Forbidden() {
  return (
    <div className="container-page flex min-h-[calc(100vh-4.5rem)] flex-col items-center justify-center gap-8 py-16 text-center">
      <motion.div
        initial={{ opacity: 0, scale: 0.85 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{ duration: 0.6, ease: [0.25, 1, 0.5, 1] }}
        className="flex h-32 w-32 items-center justify-center rounded-[2rem] bg-clay-50 text-clay-500 shadow-(--shadow-lifted) dark:bg-clay-500/15 dark:text-clay-400"
      >
        <ShieldAlert size={48} strokeWidth={1.5} />
      </motion.div>

      <div className="flex flex-col items-center gap-3">
        <span className="font-display text-7xl text-stone-200 dark:text-stone-700">403</span>
        <h1 className="font-display text-2xl text-stone-900 sm:text-3xl dark:text-stone-50">Доступ ограничен</h1>
        <p className="max-w-md text-balance text-stone-500 dark:text-stone-400">
          У вас недостаточно прав для просмотра этой страницы. Если вы считаете, что это ошибка, свяжитесь с
          поддержкой.
        </p>
      </div>

      <div className="flex flex-wrap items-center justify-center gap-3">
        <Link to="/">
          <Button leftIcon={<ArrowLeft size={16} />}>На главную</Button>
        </Link>
        <Link to="/contact">
          <Button variant="outline" leftIcon={<LifeBuoy size={16} />}>
            Связаться с поддержкой
          </Button>
        </Link>
      </div>
    </div>
  );
}
