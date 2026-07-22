import { useState } from "react";
import { useTranslation } from "react-i18next";
import { MessageSquare, Star } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { formatDate } from "@/lib/utils";
import { useAdminReviews, type AdminReviewDto } from "@/data/adminEntities";

const PAGE_SIZE = 10;

function RatingStars({ rating }: { rating: number }) {
  return (
    <span className="flex items-center gap-0.5">
      {Array.from({ length: 5 }, (_, i) => (
        <Star
          key={i}
          size={14}
          className={i < rating ? "fill-harvest-400 text-harvest-400" : "text-stone-200 dark:text-stone-700"}
        />
      ))}
    </span>
  );
}

export function AdminReviews() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const { reviews, loading, error } = useAdminReviews();

  if (loading) return <PageLoader />;

  if (error || !reviews) {
    return <EmptyState icon={<MessageSquare size={26} />} title={t("reviews.errorTitle")} description={error ?? t("reviews.errorDescription")} />;
  }

  if (reviews.length === 0) {
    return <EmptyState icon={<MessageSquare size={26} />} title={t("reviews.emptyTitle")} description={t("reviews.emptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(reviews.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminReviewDto[] = reviews.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  return (
    <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
              <th className="px-6 py-4 font-medium">{t("reviews.columns.order")}</th>
              <th className="px-6 py-4 font-medium">{t("reviews.columns.customer")}</th>
              <th className="px-6 py-4 font-medium">{t("reviews.columns.farmer")}</th>
              <th className="px-6 py-4 font-medium">{t("reviews.columns.rating")}</th>
              <th className="px-6 py-4 font-medium">{t("reviews.columns.comment")}</th>
              <th className="px-6 py-4 font-medium">{t("reviews.columns.createdAt")}</th>
            </tr>
          </thead>
          <tbody>
            {pageItems.map((review) => (
              <tr key={review.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{t("reviews.orderLabel", { id: review.orderId })}</td>
                <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{t("reviews.customerLabel", { id: review.customerId })}</td>
                <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{t("reviews.farmerLabel", { id: review.farmerId })}</td>
                <td className="px-6 py-4">
                  <RatingStars rating={review.rating} />
                </td>
                <td className="max-w-80 truncate px-6 py-4 text-stone-500 dark:text-stone-400">{review.comment ?? "—"}</td>
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
  );
}
