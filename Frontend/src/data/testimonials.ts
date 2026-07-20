import { useTranslation } from "react-i18next";
import type { Testimonial } from "@/types";

type TestimonialBase = Omit<Testimonial, "role" | "quote">;

const testimonialsBase: TestimonialBase[] = [
  { id: 1, name: "Фарида Назарова", rating: 5 },
  { id: 2, name: "Рустам Абдуллоев", rating: 5 },
  { id: 3, name: "Гулбахор Раҳимова", rating: 5 },
  { id: 4, name: "Шерали Юсупов", rating: 4 },
  { id: 5, name: "Наргис Каримова", rating: 5 },
];

export function useTestimonials(): Testimonial[] {
  const { t } = useTranslation("data");
  return testimonialsBase.map((item) => ({
    ...item,
    role: t(`testimonials.${item.id}.role`),
    quote: t(`testimonials.${item.id}.quote`),
  }));
}
