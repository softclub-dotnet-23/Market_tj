import { Link } from "react-router-dom";
import { ArrowUpRight } from "lucide-react";
import { SectionHeading } from "@/components/ui/SectionHeading";
import { FarmerCard } from "@/components/product/FarmerCard";
import { Carousel } from "@/components/ui/Carousel";
import { farmers } from "@/data/farmers";

const featured = [...farmers].sort((a, b) => b.rating - a.rating).slice(0, 6);

export function BestFarmers() {
  return (
    <section className="container-page py-14 sm:py-20">
      <SectionHeading
        eyebrow="Фермеры"
        align="left"
        title="Лучшие фермерские хозяйства"
        description="Проверенные хозяйства с высоким рейтингом и стабильным качеством поставок."
        action={
          <Link
            to="/catalog"
            className="inline-flex items-center gap-1.5 text-sm font-semibold text-grove-700 transition hover:text-grove-800 dark:text-grove-400 dark:hover:text-grove-300"
          >
            Все фермеры
            <ArrowUpRight size={16} />
          </Link>
        }
      />

      <Carousel className="mt-10" slideClassName="w-[85%] sm:w-[45%] lg:w-[31%]">
        {featured.map((farmer) => (
          <FarmerCard key={farmer.id} farmer={farmer} />
        ))}
      </Carousel>
    </section>
  );
}
