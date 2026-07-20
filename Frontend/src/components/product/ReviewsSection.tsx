import { useTranslation } from "react-i18next";
import { BadgeCheck, ThumbsUp } from "lucide-react";
import type { Review } from "@/types";
import { Avatar } from "@/components/ui/Avatar";
import { RatingStars } from "@/components/ui/RatingStars";
import { getCustomerPhoto } from "@/assets/photos";
import { formatDate } from "@/lib/utils";

function RatingBreakdown({ reviews, rating, count }: { reviews: Review[]; rating: number; count: number }) {
  const { t } = useTranslation("product");
  const distribution = [5, 4, 3, 2, 1].map((star) => ({
    star,
    count: reviews.filter((r) => r.rating === star).length,
  }));
  const max = Math.max(1, ...distribution.map((d) => d.count));

  return (
    <div className="flex flex-col gap-6 rounded-3xl border border-stone-100 bg-white p-6 sm:flex-row sm:items-center sm:gap-10 dark:border-stone-800 dark:bg-stone-900">
      <div className="flex shrink-0 flex-col items-center gap-1.5 sm:border-r sm:border-stone-100 sm:pr-10 dark:sm:border-stone-800">
        <span className="font-display text-5xl text-stone-900 dark:text-stone-50">{rating.toFixed(1)}</span>
        <RatingStars rating={rating} size={16} />
        <span className="text-sm text-stone-400 dark:text-stone-500">{t("reviewsCount", { count })}</span>
      </div>
      <div className="flex flex-1 flex-col gap-1.5">
        {distribution.map((d) => (
          <div key={d.star} className="flex items-center gap-3">
            <span className="w-3 text-xs text-stone-500 dark:text-stone-400">{d.star}</span>
            <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-stone-100 dark:bg-stone-800">
              <div
                className="h-full rounded-full bg-harvest-400"
                style={{ width: `${(d.count / max) * 100}%` }}
              />
            </div>
            <span className="w-6 text-right text-xs text-stone-400 dark:text-stone-500">{d.count}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

export function ReviewsSection({ reviews, rating, count }: { reviews: Review[]; rating: number; count: number }) {
  const { t } = useTranslation("product");
  return (
    <div className="flex flex-col gap-6">
      <RatingBreakdown reviews={reviews} rating={rating} count={count} />

      <div className="flex flex-col divide-y divide-stone-100 dark:divide-stone-800">
        {reviews.map((review) => (
          <div key={review.id} className="flex gap-4 py-6 first:pt-0">
            <Avatar name={review.customerName} src={getCustomerPhoto(review.id)} size={44} />
            <div className="flex flex-1 flex-col gap-2">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <div className="flex items-center gap-2">
                  <span className="text-sm font-semibold text-stone-800 dark:text-stone-100">{review.customerName}</span>
                  {review.verifiedPurchase && (
                    <span className="flex items-center gap-1 text-xs text-grove-600 dark:text-grove-400">
                      <BadgeCheck size={12} />
                      {t("verifiedPurchase")}
                    </span>
                  )}
                </div>
                <span className="text-xs text-stone-400 dark:text-stone-500">{formatDate(review.createdAt)}</span>
              </div>
              <RatingStars rating={review.rating} size={13} />
              <p className="text-[15px] leading-relaxed text-stone-600 dark:text-stone-300">{review.comment}</p>

              {review.farmerReply && (
                <div className="mt-1 rounded-2xl bg-stone-50 p-4 dark:bg-stone-800/60">
                  <p className="mb-1 text-xs font-semibold text-stone-700 dark:text-stone-200">{t("farmerReply")}</p>
                  <p className="text-sm text-stone-500 dark:text-stone-400">{review.farmerReply.message}</p>
                </div>
              )}

              <button className="mt-1 flex w-fit items-center gap-1.5 text-xs text-stone-400 transition hover:text-grove-700 dark:text-stone-500 dark:hover:text-grove-400">
                <ThumbsUp size={12} />
                {t("helpful", { count: review.helpfulCount })}
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
