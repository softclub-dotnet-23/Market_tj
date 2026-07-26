import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Star } from "lucide-react";
import { Modal } from "@/components/ui/Modal";
import { Textarea } from "@/components/ui/Field";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

function StarPicker({ value, onChange }: { value: number; onChange: (rating: number) => void }) {
  const [hovered, setHovered] = useState(0);
  return (
    <div className="flex items-center gap-1.5" onMouseLeave={() => setHovered(0)}>
      {[1, 2, 3, 4, 5].map((star) => {
        const filled = (hovered || value) >= star;
        return (
          <button
            key={star}
            type="button"
            onMouseEnter={() => setHovered(star)}
            onClick={() => onChange(star)}
            className="p-0.5"
          >
            <Star
              size={28}
              className={cn("transition-colors", filled ? "text-harvest-500" : "text-stone-200 dark:text-stone-700")}
              fill="currentColor"
            />
          </button>
        );
      })}
    </div>
  );
}

export function ReviewModal({
  open,
  onClose,
  orderNumber,
  onSubmit,
}: {
  open: boolean;
  onClose: () => void;
  orderNumber: string;
  onSubmit: (rating: number, comment: string) => Promise<void>;
}) {
  const { t } = useTranslation("customer");
  const [rating, setRating] = useState(0);
  const [comment, setComment] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [ratingError, setRatingError] = useState(false);

  const handleClose = () => {
    if (submitting) return;
    onClose();
    setRating(0);
    setComment("");
    setRatingError(false);
  };

  const handleSubmit = async () => {
    if (rating === 0) {
      setRatingError(true);
      return;
    }
    setSubmitting(true);
    try {
      await onSubmit(rating, comment.trim() || "");
      handleClose();
    } catch {
      // onSubmit уже показывает свой toast с ошибкой — оставляем модалку
      // открытой, чтобы не потерять выбранную оценку и комментарий.
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal open={open} onClose={handleClose}>
      <div className="flex flex-col gap-5">
        <div>
          <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("orders.reviewModalTitle")}</h2>
          <p className="text-sm text-stone-400 dark:text-stone-500">{t("orders.reviewModalSubtitle", { orderNumber })}</p>
        </div>

        <div className="flex flex-col gap-2">
          <span className="text-sm font-medium text-stone-700 dark:text-stone-300">{t("orders.reviewRatingLabel")}</span>
          <StarPicker
            value={rating}
            onChange={(r) => {
              setRating(r);
              setRatingError(false);
            }}
          />
          {ratingError && <span className="text-xs font-medium text-danger">{t("orders.reviewRatingRequired")}</span>}
        </div>

        <Textarea
          label={t("orders.reviewCommentLabel")}
          placeholder={t("orders.reviewCommentPlaceholder")}
          rows={4}
          value={comment}
          onChange={(e) => setComment(e.target.value)}
        />

        <Button size="lg" loading={submitting} onClick={handleSubmit} className="w-full">
          {t("orders.reviewSubmit")}
        </Button>
      </div>
    </Modal>
  );
}
