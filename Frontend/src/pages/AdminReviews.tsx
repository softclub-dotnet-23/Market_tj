import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { MessageSquare, Star, Trash2 } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { formatDate } from "@/lib/utils";
import { deleteReview, useAdminFarmers, useAdminReviews, type AdminReviewDto } from "@/data/adminEntities";

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
  const [refreshKey, setRefreshKey] = useState(0);
  const [deleting, setDeleting] = useState<AdminReviewDto | null>(null);
  const { reviews, loading, error } = useAdminReviews(refreshKey);
  const { farmers } = useAdminFarmers();

  // По прямому запросу пользователя — имя фермера, а не "Фермер #N" (тот же
  // приём, что и в AdminOrders.tsx).
  const farmNameById = new Map((farmers ?? []).map((f) => [f.id, f.farmName]));

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

  const handleDelete = async () => {
    if (!deleting) return;
    try {
      await deleteReview(deleting.id);
      toast.success(t("reviews.deleteSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("reviews.deleteError"), { description: err instanceof Error ? err.message : undefined });
    }
  };

  const renderCard = (review: AdminReviewDto) => (
    <div key={review.id} className="flex flex-col gap-2.5 rounded-2xl border border-stone-100 bg-white p-4 shadow-sm dark:border-stone-800 dark:bg-stone-900">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{t("reviews.orderLabel", { id: review.orderId })}</p>
          <p className="truncate text-xs text-stone-400 dark:text-stone-500">
            {review.customerFullName ?? t("reviews.customerLabel", { id: review.customerId })} ·{" "}
            {farmNameById.get(review.farmerId) ?? t("reviews.farmerLabel", { id: review.farmerId })}
          </p>
        </div>
        <button
          onClick={() => setDeleting(review)}
          aria-label={t("reviews.deleteAction")}
          className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
        >
          <Trash2 size={15} />
        </button>
      </div>
      <RatingStars rating={review.rating} />
      {review.comment && <p className="text-sm text-stone-600 dark:text-stone-300">{review.comment}</p>}
      <p className="text-xs text-stone-400 dark:text-stone-500">{formatDate(review.createdAt)}</p>
    </div>
  );

  return (
    <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
      <div className="flex flex-col gap-3 p-4 lg:hidden">{pageItems.map(renderCard)}</div>

      <div className="hidden overflow-x-auto lg:block">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
              <th className="px-6 py-4 font-medium">{t("reviews.columns.order")}</th>
              <th className="px-6 py-4 font-medium">{t("reviews.columns.customer")}</th>
              <th className="px-6 py-4 font-medium">{t("reviews.columns.farmer")}</th>
              <th className="px-6 py-4 font-medium">{t("reviews.columns.rating")}</th>
              <th className="px-6 py-4 font-medium">{t("reviews.columns.comment")}</th>
              <th className="px-6 py-4 font-medium">{t("reviews.columns.createdAt")}</th>
              <th className="px-6 py-4 font-medium text-right">{t("reviews.columns.actions")}</th>
            </tr>
          </thead>
          <tbody>
            {pageItems.map((review) => (
              <tr key={review.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{t("reviews.orderLabel", { id: review.orderId })}</td>
                <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{review.customerFullName ?? t("reviews.customerLabel", { id: review.customerId })}</td>
                <td className="px-6 py-4 text-stone-600 dark:text-stone-300">
                  {farmNameById.get(review.farmerId) ?? t("reviews.farmerLabel", { id: review.farmerId })}
                </td>
                <td className="px-6 py-4">
                  <RatingStars rating={review.rating} />
                </td>
                <td className="max-w-80 whitespace-normal wrap-break-word px-6 py-4 text-stone-500 dark:text-stone-400">{review.comment ?? "—"}</td>
                <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(review.createdAt)}</td>
                <td className="px-6 py-4">
                  <div className="flex items-center justify-end">
                    <button
                      onClick={() => setDeleting(review)}
                      aria-label={t("reviews.deleteAction")}
                      className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
                    >
                      <Trash2 size={15} />
                    </button>
                  </div>
                </td>
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

      <ConfirmDialog
        open={!!deleting}
        onClose={() => setDeleting(null)}
        onConfirm={handleDelete}
        title={t("reviews.deleteConfirmTitle")}
        description={t("reviews.deleteConfirmDescription")}
        confirmLabel={t("reviews.deleteAction")}
      />
    </div>
  );
}
