import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { apiGet } from "@/lib/api";
import type { Testimonial } from "@/types";

interface ReviewDto {
  id: number;
  orderId: number;
  customerId: number;
  customerFullName: string | null;
  farmerId: number;
  rating: number;
  comment: string | null;
  createdAt: string;
}

const MAX_TESTIMONIALS = 6;

// Отзывы реальные (GET /reviews, публичный эндпоинт) — раньше здесь были
// придуманные люди с придуманными цитатами. Имя покупателя теперь тоже
// настоящее (по прямому запросу пользователя 2026-07-31 — отзыв публичный,
// скрывать автора незачем), сервер резолвит его через CustomerProfile→User.
export function useTestimonials(): { testimonials: Testimonial[]; loading: boolean } {
  const { t } = useTranslation("data");
  const [reviews, setReviews] = useState<ReviewDto[] | null>(null);

  useEffect(() => {
    let cancelled = false;
    apiGet<ReviewDto[]>("/reviews")
      .then((data) => {
        if (!cancelled) setReviews(data);
      })
      .catch(() => {
        if (!cancelled) setReviews([]);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const testimonials = (reviews ?? [])
    .filter((r) => r.comment && r.comment.trim().length > 0)
    .sort((a, b) => b.rating - a.rating || new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, MAX_TESTIMONIALS)
    .map((r) => ({
      id: r.id,
      name: r.customerFullName ?? t("testimonials.genericCustomerName"),
      role: t("testimonials.verifiedPurchase"),
      quote: r.comment as string,
      rating: r.rating,
    }));

  return { testimonials, loading: reviews === null };
}
