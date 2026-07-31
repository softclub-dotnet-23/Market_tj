import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { BadgeCheck, MapPin, Package } from "lucide-react";
import type { Farmer } from "@/types";
import { Avatar } from "@/components/ui/Avatar";
import { RatingStars } from "@/components/ui/RatingStars";
import { resolveMediaUrl } from "@/lib/api";
import { cn } from "@/lib/utils";

export function FarmerCard({ farmer, className }: { farmer: Farmer; className?: string }) {
  const { t } = useTranslation("product");
  return (
    <motion.div
      whileHover={{ y: -6 }}
      transition={{ type: "spring", stiffness: 300, damping: 22 }}
      className={cn(
        "group flex flex-col overflow-hidden rounded-2xl border border-stone-100 bg-white transition-shadow duration-300 hover:shadow-(--shadow-card) dark:border-stone-800 dark:bg-stone-900",
        className,
      )}
    >
      <Link
        to={`/farmers/${farmer.id}`}
        className="relative flex aspect-[4/3.2] items-center justify-center overflow-hidden bg-linear-to-br from-grove-50 via-stone-50 to-harvest-50 dark:from-grove-950 dark:via-stone-900 dark:to-stone-900"
      >
        {farmer.avatarUrl ? (
          <motion.img
            whileHover={{ scale: 1.06 }}
            transition={{ duration: 0.5, ease: [0.25, 1, 0.5, 1] }}
            src={resolveMediaUrl(farmer.avatarUrl)}
            alt={farmer.farmName}
            loading="lazy"
            className="h-full w-full object-cover"
          />
        ) : (
          <motion.div whileHover={{ scale: 1.08 }} transition={{ duration: 0.5, ease: [0.25, 1, 0.5, 1] }}>
            <Avatar name={farmer.farmName} size={84} ring />
          </motion.div>
        )}
        {farmer.verified && (
          <span className="absolute right-3 top-3 flex items-center gap-1 rounded-full bg-white/95 px-2.5 py-1 text-xs font-semibold text-grove-700 shadow-sm backdrop-blur dark:bg-stone-800/95 dark:text-grove-400">
            <BadgeCheck size={13} />
            {t("verified")}
          </span>
        )}
      </Link>

      <div className="flex flex-1 flex-col gap-3 p-5">
        <div>
          <Link to={`/catalog?farmer=${farmer.id}`}>
            <h3 className="font-display text-lg text-stone-900 transition-colors group-hover:text-grove-800 dark:text-stone-50 dark:group-hover:text-grove-400">
              {farmer.farmName}
            </h3>
          </Link>
        </div>

        <div className="flex items-center gap-1.5 text-xs text-stone-400 dark:text-stone-500">
          <MapPin size={13} />
          {farmer.district}, {farmer.region}
        </div>

        <div className="mt-1 flex items-center justify-between border-t border-stone-100 pt-3 dark:border-stone-800">
          <RatingStars rating={farmer.rating} size={13} showValue reviewCount={farmer.reviewCount} />
          <span className="flex items-center gap-1 text-xs text-stone-500 dark:text-stone-400">
            <Package size={13} />
            {t("productsCount", { count: farmer.productCount })}
          </span>
        </div>
      </div>
    </motion.div>
  );
}
