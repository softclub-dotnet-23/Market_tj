import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { MessageSquare, Sparkles, Star } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { Button } from "@/components/ui/Button";
import { Checkbox } from "@/components/ui/Field";
import { formatDate } from "@/lib/utils";
import { replyToReview, setFarmerAutoReply, useFarmerProfile, useFarmerReviews, type FarmerReviewDto } from "@/data/farmer";

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

// Ответ фермера на отзыв — по прямому запросу пользователя ("сделай так,
// чтобы в отзыв клиентов фермер мог ответить"). Вручную, текстовым полем
// прямо под отзывом — предложение AI-ассистентом того же самого ответа
// (propose_reply_review) идёт через ТОТ ЖЕ PATCH /reviews/{id}/reply, эта
// форма и AI-подтверждение — два входа в одно и то же действие.
function ReplyForm({ review, onSaved, onCancel }: { review: FarmerReviewDto; onSaved: () => void; onCancel: () => void }) {
  const { t } = useTranslation("farmer");
  const [text, setText] = useState(review.farmerReply ?? "");
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async () => {
    if (!text.trim()) return;
    setSubmitting(true);
    try {
      await replyToReview(review.id, text.trim());
      toast.success(t("reviews.replySuccess"));
      onSaved();
    } catch (err) {
      toast.error(t("reviews.replyError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="mt-2 flex flex-col gap-2 rounded-2xl bg-stone-50 p-3 dark:bg-stone-800/60">
      <textarea
        value={text}
        onChange={(e) => setText(e.target.value)}
        placeholder={t("reviews.replyPlaceholder")}
        rows={2}
        className="w-full resize-none rounded-xl border border-stone-200 bg-white p-2.5 text-sm outline-none focus:border-grove-400 dark:border-stone-700 dark:bg-stone-900 dark:text-stone-100"
      />
      <div className="flex justify-end gap-2">
        <Button variant="outline" onClick={onCancel} disabled={submitting}>
          {t("reviews.replyCancel")}
        </Button>
        <Button onClick={handleSubmit} loading={submitting} disabled={!text.trim()}>
          {t("reviews.replySubmit")}
        </Button>
      </div>
    </div>
  );
}

function ReplyBlock({ review, onSaved }: { review: FarmerReviewDto; onSaved: () => void }) {
  const { t } = useTranslation("farmer");
  const [editing, setEditing] = useState(false);

  if (editing) {
    return <ReplyForm review={review} onSaved={() => { setEditing(false); onSaved(); }} onCancel={() => setEditing(false)} />;
  }

  if (review.farmerReply) {
    return (
      <div className="mt-2 flex flex-col gap-1.5 rounded-2xl bg-stone-50 p-3 dark:bg-stone-800/60">
        <div className="flex items-center justify-between gap-2">
          <p className="text-xs font-semibold text-stone-700 dark:text-stone-200">{t("reviews.yourReply")}</p>
          <button onClick={() => setEditing(true)} className="text-xs font-medium text-grove-700 hover:underline dark:text-grove-400">
            {t("reviews.editReplyAction")}
          </button>
        </div>
        <p className="text-sm text-stone-500 dark:text-stone-400">{review.farmerReply}</p>
      </div>
    );
  }

  return (
    <button
      onClick={() => setEditing(true)}
      className="mt-1 w-fit text-xs font-medium text-grove-700 hover:underline dark:text-grove-400"
    >
      {t("reviews.replyAction")}
    </button>
  );
}

export function FarmerReviews() {
  const { t } = useTranslation("farmer");
  const [page, setPage] = useState(1);
  const [refreshKey, setRefreshKey] = useState(0);
  const [togglingAutoReply, setTogglingAutoReply] = useState(false);
  const { profile, loading: profileLoading, error: profileError } = useFarmerProfile(refreshKey);
  const { reviews, loading: reviewsLoading, error: reviewsError } = useFarmerReviews(profile?.id ?? null, refreshKey);

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

  const totalPages = Math.max(1, Math.ceil(reviews.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: FarmerReviewDto[] = reviews.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);
  const averageRating = reviews.length ? reviews.reduce((sum, r) => sum + r.rating, 0) / reviews.length : 0;
  const bump = () => setRefreshKey((k) => k + 1);

  const handleToggleAutoReply = async () => {
    setTogglingAutoReply(true);
    const nextEnabled = !profile.autoReplyToReviewsEnabled;
    try {
      await setFarmerAutoReply(profile.id, nextEnabled);
      toast.success(nextEnabled ? t("reviews.autoReplyOnSuccess") : t("reviews.autoReplyOffSuccess"));
      bump();
    } catch (err) {
      toast.error(t("reviews.autoReplyError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setTogglingAutoReply(false);
    }
  };

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col gap-4 rounded-3xl border border-stone-100 bg-white p-6 sm:flex-row sm:items-center sm:justify-between dark:border-stone-800 dark:bg-stone-900">
        <div className="flex items-center gap-4">
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

        <div className="flex items-start gap-2.5 rounded-2xl bg-stone-50 p-3.5 sm:max-w-xs dark:bg-stone-800/60">
          <Sparkles size={16} className="mt-0.5 shrink-0 text-grove-600 dark:text-grove-400" />
          <Checkbox
            label={
              <span>
                <span className="font-medium text-stone-700 dark:text-stone-200">{t("reviews.autoReplyLabel")}</span>
                <span className="mt-0.5 block text-xs text-stone-400 dark:text-stone-500">{t("reviews.autoReplyHint")}</span>
              </span>
            }
            checked={profile.autoReplyToReviewsEnabled}
            onChange={handleToggleAutoReply}
            disabled={togglingAutoReply}
          />
        </div>
      </div>

      {reviews.length === 0 ? (
        <EmptyState icon={<MessageSquare size={26} />} title={t("reviews.emptyTitle")} description={t("reviews.emptyDescription")} />
      ) : (
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
              <ReplyBlock review={review} onSaved={bump} />
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
                  <td className="max-w-80 whitespace-normal wrap-break-word px-6 py-4 text-stone-500 dark:text-stone-400">
                    {review.comment ?? "—"}
                    <ReplyBlock review={review} onSaved={bump} />
                  </td>
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
      )}
    </div>
  );
}
