import { useState } from "react";
import { useTranslation } from "react-i18next";
import { MessageSquare, Star } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { formatDate } from "@/lib/utils";
import { useFarmerProfile, useFarmerReviews, type FarmerReviewDto } from "@/data/farmer";

const PAGE_SIZE = 10;

function RatingStars({ rating }: { rating: number }) {
  return (
    <span className="flex items-center gap-0.5">
      {Array.from({ length: 5 }, (_, i) => (
        <Star key={i} size={14} className={i < rating ? "fill-harvest-400 text-harvest-400" : "text-stone-200 dark:text-stone-700"} />
      ))}
    </span>
  );
}

export function FarmerReviews() {
  const { t } = useTranslation("farmer");
  const [page, setPage] = useState(1);
  const { profile, loading: profileLoading, error: profileError } = useFarmerProfile();
  const { reviews, loading: reviewsLoading, error: reviewsError } = useFarmerReviews(profile?.id ?? null);

  if (profileLoading || (profile && reviewsLoading)) return <PageLoader />;

  if (profileError || reviewsError || !profile || !reviews) {
    return (
      <EmptyState
        icon={<MessageSquare size={26} />}
        title={t("reviews.errorTitle")}
        description={profileError ?? reviewsError ?? t("reviews.errorDescription")}
      />
    );
  }

  if (reviews.length === 0) {
    return <EmptyState icon={<MessageSquare size={26} />} title={t("reviews.emptyTitle")} description={t("reviews.emptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(reviews.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: FarmerReviewDto[] = reviews.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);
  const averageRating = reviews.reduce((sum, r) => sum + r.rating, 0) / reviews.length;

  return (
    <div className="flex flex-col gap-5">
      <div className="flex items-center gap-4 rounded-3xl border border-stone-100 bg-white p-6 dark:border-stone-800 dark:bg-stone-900">
        <span className="flex h-12 w-12 items-center justify-center rounded-2xl bg-harvest-100 text-harvest-700 dark:bg-harvest-900 dark:text-harvest-200">
          <Star size={22} className="fill-current" />
        </span>
        <div>
          <p className="text-sm text-stone-500 dark:text-stone-400">{t("reviews.averageRating")}</p>
          <p className="font-display text-2xl text-stone-900 dark:text-stone-50">
            {averageRating.toFixed(1)} <span className="text-sm font-normal text-stone-400 dark:text-stone-500">/ 5 ({reviews.length})</span>
          </p>
        </div>
      </div>

      <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
        <div className="flex flex-col gap-3 p-4 lg:hidden">
          {pageItems.map((review) => (
            <div key={review.id} className="flex flex-col gap-2 rounded-2xl border border-stone-100 p-4 dark:border-stone-800">
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{t("reviews.orderLabel", { id: review.orderId })}</p>
                  <p className="truncate text-xs text-stone-400 dark:text-stone-500">{review.customerFullName ?? t("reviews.customerLabel", { id: review.customerId })}</p>
                </div>
                <RatingStars rating={review.rating} />
              </div>
              {review.comment && <p className="text-sm text-stone-600 dark:text-stone-300">{review.comment}</p>}
              <p className="text-xs text-stone-400 dark:text-stone-500">{formatDate(review.createdAt)}</p>
            </div>
          ))}
        </div>

        <div className="hidden overflow-x-auto lg:block">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
                <th className="px-6 py-4 font-medium">{t("reviews.columns.order")}</th>
                <th className="px-6 py-4 font-medium">{t("reviews.columns.customer")}</th>
                <th className="px-6 py-4 font-medium">{t("reviews.columns.rating")}</th>
                <th className="px-6 py-4 font-medium">{t("reviews.columns.comment")}</th>
                <th className="px-6 py-4 font-medium">{t("reviews.columns.createdAt")}</th>
              </tr>
            </thead>
            <tbody>
              {pageItems.map((review) => (
                <tr key={review.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                  <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{t("reviews.orderLabel", { id: review.orderId })}</td>
                  <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{review.customerFullName ?? t("reviews.customerLabel", { id: review.customerId })}</td>
                  <td className="px-6 py-4">
                    <RatingStars rating={review.rating} />
                  </td>
                  <td className="max-w-80 whitespace-normal wrap-break-word px-6 py-4 text-stone-500 dark:text-stone-400">{review.comment ?? "—"}</td>
                  <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(review.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {totalPages > 1 && (
          <div className="border-t border-stone-100 p-4 dark:border-stone-800">
            <Pagination page={currentPage} totalPages={totalPages} onPageChange={setPage} />
          </div>
        )}
      </div>
    </div>
  );
}
