import { useTranslation } from "react-i18next";
import { SectionHeading } from "@/components/ui/SectionHeading";
import { FarmerCard } from "@/components/product/FarmerCard";
import { Carousel } from "@/components/ui/Carousel";
import { useFarmers } from "@/data/farmers";

export function BestFarmers() {
  const { t } = useTranslation("sections");
  const farmers = useFarmers();
  const featured = [...farmers].sort((a, b) => b.rating - a.rating).slice(0, 6);
  return (
    <section className="container-page py-14 sm:py-20">
      <SectionHeading
        eyebrow={t("bestFarmers.eyebrow")}
        align="left"
        title={t("bestFarmers.title")}
        description={t("bestFarmers.description")}
      />

      <Carousel className="mt-10" slideClassName="w-[85%] sm:w-[45%] lg:w-[31%]">
        {featured.map((farmer) => (
          <FarmerCard key={farmer.id} farmer={farmer} />
        ))}
      </Carousel>
    </section>
  );
}
