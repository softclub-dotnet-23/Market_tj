import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { BadgeCheck, MapPin, Package } from "lucide-react";
import type { Farmer } from "@/types";
import { RatingStars } from "@/components/ui/RatingStars";
import { farmerPhotos } from "@/assets/photos";
import { cn } from "@/lib/utils";

export function FarmerCard({ farmer, className }: { farmer: Farmer; className?: string }) {
  return (
    <motion.div
      whileHover={{ y: -6 }}
      transition={{ type: "spring", stiffness: 300, damping: 22 }}
      className={cn(
        "group flex flex-col overflow-hidden rounded-2xl border border-stone-100 bg-white transition-shadow duration-300 hover:shadow-(--shadow-card) dark:border-stone-800 dark:bg-stone-900",
        className,
      )}
    >
      <Link to={`/catalog?farmer=${farmer.id}`} className="relative block aspect-[4/3.2] overflow-hidden bg-stone-100 dark:bg-stone-800">
        <motion.img
          src={farmerPhotos[farmer.id]}
          alt={farmer.ownerName}
          loading="lazy"
          whileHover={{ scale: 1.06 }}
          transition={{ duration: 0.5, ease: [0.25, 1, 0.5, 1] }}
          className="h-full w-full object-cover object-top"
        />
        {farmer.verified && (
          <span className="absolute right-3 top-3 flex items-center gap-1 rounded-full bg-white/95 px-2.5 py-1 text-xs font-semibold text-grove-700 shadow-sm backdrop-blur dark:bg-stone-800/95 dark:text-grove-400">
            <BadgeCheck size={13} />
            Проверен
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
          <p className="text-sm text-stone-500 dark:text-stone-400">{farmer.ownerName}</p>
        </div>

        <div className="flex items-center gap-1.5 text-xs text-stone-400 dark:text-stone-500">
          <MapPin size={13} />
          {farmer.district}, {farmer.region}
        </div>

        <div className="mt-1 flex items-center justify-between border-t border-stone-100 pt-3 dark:border-stone-800">
          <RatingStars rating={farmer.rating} size={13} showValue reviewCount={farmer.reviewCount} />
          <span className="flex items-center gap-1 text-xs text-stone-500 dark:text-stone-400">
            <Package size={13} />
            {farmer.productCount} товаров
          </span>
        </div>
      </div>
    </motion.div>
  );
}
