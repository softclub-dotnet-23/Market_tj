import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { BadgeCheck, MapPin, MessageCircle, Package, Star } from "lucide-react";
import type { Farmer } from "@/types";
import { Avatar } from "@/components/ui/Avatar";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { farmerPhotos } from "@/assets/photos";

export function FarmerProfileCard({ farmer }: { farmer: Farmer }) {
  const { t } = useTranslation("product");
  return (
    <div className="flex flex-col gap-5 rounded-3xl border border-stone-100 bg-white p-6 sm:flex-row sm:items-center sm:justify-between dark:border-stone-800 dark:bg-stone-900">
      <div className="flex items-center gap-4">
        <Avatar name={farmer.ownerName} src={farmerPhotos[farmer.id]} size={60} />
        <div className="flex flex-col gap-1">
          <div className="flex items-center gap-2">
            <h3 className="font-display text-lg text-stone-900 dark:text-stone-50">{farmer.farmName}</h3>
            {farmer.verified && (
              <span title={t("verifiedFarmerTitle")}>
                <BadgeCheck size={16} className="text-grove-600 dark:text-grove-400" />
              </span>
            )}
          </div>
          <p className="text-sm text-stone-500 dark:text-stone-400">{farmer.ownerName}</p>
          <div className="mt-1 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-stone-400 dark:text-stone-500">
            <span className="flex items-center gap-1">
              <MapPin size={12} />
              {farmer.district}
            </span>
            <span className="flex items-center gap-1">
              <Star size={12} className="text-harvest-500" fill="currentColor" />
              {farmer.rating.toFixed(1)} ({farmer.reviewCount})
            </span>
            <span className="flex items-center gap-1">
              <Package size={12} />
              {t("productsCount", { count: farmer.productCount })}
            </span>
          </div>
          <div className="mt-1.5 flex flex-wrap gap-1.5">
            {farmer.tags.map((tag) => (
              <Badge key={tag} variant="stone" className="text-[11px]">
                {tag}
              </Badge>
            ))}
          </div>
        </div>
      </div>

      <div className="flex shrink-0 gap-2">
        <Link to={`/catalog?farmer=${farmer.id}`}>
          <Button variant="outline" size="sm">
            {t("allProducts")}
          </Button>
        </Link>
        <Button variant="ghost" size="sm" leftIcon={<MessageCircle size={14} />}>
          {t("writeMessage")}
        </Button>
      </div>
    </div>
  );
}
