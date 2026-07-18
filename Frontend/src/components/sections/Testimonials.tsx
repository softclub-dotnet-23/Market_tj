import { useTranslation } from "react-i18next";
import { Quote } from "lucide-react";
import { SectionHeading } from "@/components/ui/SectionHeading";
import { Carousel } from "@/components/ui/Carousel";
import { Avatar } from "@/components/ui/Avatar";
import { RatingStars } from "@/components/ui/RatingStars";
import { useTestimonials } from "@/data/testimonials";
import { getCustomerPhoto } from "@/assets/photos";

export function Testimonials() {
  const { t } = useTranslation("sections");
  const testimonials = useTestimonials();
  return (
    <section className="container-page py-14 sm:py-20">
      <SectionHeading
        eyebrow={t("testimonials.eyebrow")}
        title={t("testimonials.title")}
      />

      <Carousel className="mt-12" slideClassName="w-[85%] sm:w-[55%] lg:w-[36%]" showDots>
        {testimonials.map((item) => (
          <div
            key={item.id}
            className="flex h-full flex-col justify-between gap-6 rounded-2xl border border-stone-100 bg-white p-7 dark:border-stone-800 dark:bg-stone-900"
          >
            <Quote size={28} className="text-grove-200 dark:text-grove-800" fill="currentColor" />
            <p className="flex-1 text-[15px] leading-relaxed text-stone-600 dark:text-stone-300">"{item.quote}"</p>
            <div className="flex items-center gap-3 border-t border-stone-100 pt-5 dark:border-stone-800">
              <Avatar name={item.name} src={getCustomerPhoto(item.id)} size={44} />
              <div>
                <p className="text-sm font-semibold text-stone-800 dark:text-stone-100">{item.name}</p>
                <p className="text-xs text-stone-400 dark:text-stone-500">{item.role}</p>
              </div>
              <RatingStars rating={item.rating} size={12} className="ml-auto" />
            </div>
          </div>
        ))}
      </Carousel>
    </section>
  );
}
