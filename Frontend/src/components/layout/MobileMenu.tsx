import { Link, NavLink } from "react-router-dom";
import { motion } from "framer-motion";
import { Leaf, User, UserPlus, X } from "lucide-react";
import { createPortal } from "react-dom";
import { categories } from "@/data/categories";
import { cn } from "@/lib/utils";

const NAV_LINKS = [
  { to: "/", label: "Главная", end: true },
  { to: "/catalog", label: "Каталог" },
  { to: "/about", label: "О нас" },
  { to: "/contact", label: "Контакты" },
];

export function MobileMenu({ onClose }: { onClose: () => void }) {
  return createPortal(
    <div className="fixed inset-0 z-100 lg:hidden">
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        onClick={onClose}
        className="absolute inset-0 bg-stone-950/50 backdrop-blur-sm"
      />
      <motion.div
        initial={{ x: "100%" }}
        animate={{ x: 0 }}
        exit={{ x: "100%" }}
        transition={{ duration: 0.32, ease: [0.25, 1, 0.5, 1] }}
        className="absolute right-0 top-0 flex h-full w-[85vw] max-w-sm flex-col bg-white p-6 dark:bg-stone-900"
      >
        <div className="flex items-center justify-between">
          <Link to="/" onClick={onClose} className="flex items-center gap-2">
            <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-grove-700 text-white">
              <Leaf size={18} />
            </span>
            <span className="font-display text-xl text-stone-900 dark:text-stone-50">
              Market<span className="text-grove-600 dark:text-grove-400">.tj</span>
            </span>
          </Link>
          <button
            onClick={onClose}
            aria-label="Закрыть меню"
            className="flex h-9 w-9 items-center justify-center rounded-full bg-stone-100 text-stone-500 dark:bg-stone-800 dark:text-stone-400"
          >
            <X size={16} />
          </button>
        </div>

        <nav className="mt-8 flex flex-col gap-1">
          {NAV_LINKS.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              end={link.end}
              onClick={onClose}
              className={({ isActive }) =>
                cn(
                  "rounded-xl px-3 py-3 font-display text-lg text-stone-800 transition hover:bg-stone-50 dark:text-stone-100 dark:hover:bg-stone-800",
                  isActive && "text-grove-700 dark:text-grove-400",
                )
              }
            >
              {link.label}
            </NavLink>
          ))}
        </nav>

        <div className="mt-6 border-t border-stone-100 pt-6 dark:border-stone-800">
          <p className="mb-3 px-3 text-xs font-semibold uppercase tracking-wide text-stone-400 dark:text-stone-500">Категории</p>
          <div className="flex flex-col gap-0.5">
            {categories.map((category) => (
              <Link
                key={category.id}
                to={`/catalog?category=${category.slug}`}
                onClick={onClose}
                className="rounded-xl px-3 py-2.5 text-sm text-stone-600 transition hover:bg-stone-50 hover:text-grove-700 dark:text-stone-300 dark:hover:bg-stone-800 dark:hover:text-grove-400"
              >
                {category.name}
              </Link>
            ))}
          </div>
        </div>

        <div className="mt-auto flex flex-col gap-2.5 border-t border-stone-100 pt-6 dark:border-stone-800">
          <Link
            to="/login"
            onClick={onClose}
            className="flex h-11 items-center justify-center gap-2 rounded-xl border border-stone-200 text-sm font-medium text-stone-700 dark:border-stone-700 dark:text-stone-200"
          >
            <User size={16} />
            Войти
          </Link>
          <Link
            to="/register"
            onClick={onClose}
            className="flex h-11 items-center justify-center gap-2 rounded-xl bg-grove-700 text-sm font-medium text-white"
          >
            <UserPlus size={16} />
            Регистрация
          </Link>
        </div>
      </motion.div>
    </div>,
    document.body,
  );
}
