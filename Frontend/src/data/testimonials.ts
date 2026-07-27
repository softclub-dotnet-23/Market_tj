import { useTranslation } from "react-i18next";
import type { Testimonial } from "@/types";

type TestimonialBase = Omit<Testimonial, "name" | "role" | "quote">;

const testimonialsBase: TestimonialBase[] = [
  { id: 1, rating: 5 },
  { id: 2, rating: 5 },
  { id: 3, rating: 5 },
  { id: 4, rating: 4 },
  { id: 5, rating: 5 },
];

export function useTestimonials(): Testimonial[] {
  const { t } = useTranslation("data");
  return testimonialsBase.map((item) => ({
    ...item,
    name: t(`testimonials.${item.id}.name`),
    role: t(`testimonials.${item.id}.role`),
    quote: t(`testimonials.${item.id}.quote`),
  }));
}
